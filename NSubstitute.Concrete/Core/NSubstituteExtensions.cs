using System;
using System.Collections.Concurrent;

namespace NSubstitute.Concrete.Core;

/// <summary>
/// Extension to NSubstitute that enables mocking of concrete classes using Harmony runtime patching
/// </summary>
public static class NSubstituteExtensions
{
    private static readonly ConcurrentDictionary<object, HarmonyMethodInterceptor> _interceptors
        = new ConcurrentDictionary<object, HarmonyMethodInterceptor>();

    /// <summary>
    /// Creates a substitute for a concrete class using Harmony runtime patching.
    /// This allows direct method calls without needing .Call() wrapper.
    /// </summary>
    public static T ForConcrete<T>(params object[] constructorArguments) where T : class
    {
        var type = typeof(T);

        // For interfaces or abstract classes, use standard NSubstitute
        if (type.IsInterface || type.IsAbstract)
        {
            return Substitute.For<T>(constructorArguments);
        }

        // Create the actual instance
        T instance;
        if (constructorArguments?.Length > 0)
        {
            instance = (T)Activator.CreateInstance(type, constructorArguments);
        }
        else
        {
            instance = Activator.CreateInstance<T>();
        }

        // Create and register the Harmony interceptor
        var interceptor = new HarmonyMethodInterceptor(type);
        interceptor.Initialize(instance);

        _interceptors[instance] = interceptor;

        // Also register with the ConcreteExtensions so Setup methods work
        ConcreteExtensions.RegisterInterceptor(instance, interceptor);

        return instance;
    }

    /// <summary>
    /// Get the Harmony interceptor for a substitute (internal use)
    /// </summary>
    internal static HarmonyMethodInterceptor GetHarmonyInterceptor<T>(T substitute) where T : class
    {
        _interceptors.TryGetValue(substitute, out var interceptor);
        return interceptor;
    }

    /// <summary>
    /// Remove a specific substitute from the registry and cleanup Harmony patches
    /// </summary>
    internal static void UnregisterInterceptor(object substitute)
    {
        if (_interceptors.TryRemove(substitute, out var interceptor))
        {
            interceptor.Cleanup();
        }
    }

    /// <summary>
    /// Clear all registered interceptors and Harmony patches
    /// </summary>
    internal static void ClearAllInterceptors()
    {
        foreach (var interceptor in _interceptors.Values)
        {
            interceptor.Cleanup();
        }
        _interceptors.Clear();
    }

    /// <summary>
    /// Get the count of registered Harmony interceptors
    /// </summary>
    internal static int GetInterceptorCount()
    {
        return _interceptors.Count;
    }
}