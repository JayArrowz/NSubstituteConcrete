using System;
using System.Collections.Concurrent;
using NSubstitute.Concrete.Utilities;

namespace NSubstitute.Concrete.Core;

/// <summary>
/// Extension to NSubstitute that enables mocking of concrete classes using Harmony runtime patching
/// </summary>
public static class NSubstituteExtensions
{
    // Keyed by identity: a substituted class that compares by value would otherwise let two
    // distinct substitutes share one interceptor.
    private static readonly ConcurrentDictionary<object, HarmonyMethodInterceptor> _interceptors
        = new ConcurrentDictionary<object, HarmonyMethodInterceptor>(ReferenceComparer.Instance);

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
    /// Get the Harmony interceptor for a substitute, or <c>null</c> if it is not one of ours.
    /// <para>
    /// Public because a host library layering its own setup and verification over this one needs the
    /// interceptor to patch further members, install a fallback and read recorded calls.
    /// </para>
    /// </summary>
    public static HarmonyMethodInterceptor GetHarmonyInterceptor<T>(T substitute) where T : class
        => GetHarmonyInterceptor((object)substitute);

    /// <summary>
    /// Get the Harmony interceptor for a substitute, or <c>null</c> if it is not one of ours.
    /// </summary>
    public static HarmonyMethodInterceptor GetHarmonyInterceptor(object substitute)
    {
        if (substitute == null) return null;
        _interceptors.TryGetValue(substitute, out var interceptor);
        return interceptor;
    }

    /// <summary>
    /// Remove a specific substitute from the registry and cleanup Harmony patches
    /// </summary>
    public static void UnregisterInterceptor(object substitute)
    {
        if (_interceptors.TryRemove(substitute, out var interceptor))
        {
            interceptor.Cleanup();
        }
    }

    /// <summary>
    /// Clear all registered interceptors and Harmony patches
    /// </summary>
    public static void ClearAllInterceptors()
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
    public static int GetInterceptorCount()
    {
        return _interceptors.Count;
    }
}