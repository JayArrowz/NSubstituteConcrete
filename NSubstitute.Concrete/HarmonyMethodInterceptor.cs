using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NSubstitute.Concrete;

/// <summary>
/// Harmony-based method interceptor that patches methods at runtime
/// </summary>
public class HarmonyMethodInterceptor : ConcreteMethodInterceptor
{
    private readonly Harmony _harmony;
    private readonly Type _targetType;
    private readonly List<MethodBase> _patchedMethods = new List<MethodBase>();
    private static readonly Dictionary<object, HarmonyMethodInterceptor> _instanceInterceptors = new Dictionary<object, HarmonyMethodInterceptor>();

    public object ProxyInstance { get; private set; }

    public HarmonyMethodInterceptor(Type targetType)
    {
        _targetType = targetType;
        _harmony = new Harmony($"NSubstitute.Concrete.{Guid.NewGuid()}");
    }

    public void Initialize(object instance)
    {
        ProxyInstance = instance;
        lock (_instanceInterceptors)
        {
            _instanceInterceptors[instance] = this;
        }
    }

    public void PatchMethod(MethodInfo method)
    {
        if (_patchedMethods.Contains(method))
            return;

        // Choose the right prefix based on whether method returns void
        string prefixName = method.ReturnType == typeof(void)
            ? nameof(VoidPrefixInterceptor)
            : nameof(PrefixInterceptor);

        var prefix = typeof(HarmonyMethodInterceptor).GetMethod(
            prefixName,
            BindingFlags.Static | BindingFlags.NonPublic);

        _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
        _patchedMethods.Add(method);
    }

    public void PatchProperty(PropertyInfo property)
    {
        if (property.CanRead && property.GetGetMethod() is MethodInfo getter)
        {
            PatchMethod(getter);
        }

        if (property.CanWrite && property.GetSetMethod() is MethodInfo setter)
        {
            PatchMethod(setter);
        }
    }

    /// <summary>
    /// Harmony prefix that intercepts method calls with return values
    /// </summary>
    private static bool PrefixInterceptor(
        object __instance,
        MethodBase __originalMethod,
        object[] __args,
        ref object __result)
    {
        // Find the interceptor for this instance
        HarmonyMethodInterceptor interceptor = null;
        lock (_instanceInterceptors)
        {
            if (!_instanceInterceptors.TryGetValue(__instance, out interceptor))
            {
                // No interceptor configured, run original method
                return true;
            }
        }

        // Always record the call for verification
        interceptor.RecordCall(__originalMethod, __args);

        // Check if we have a configuration for this method
        var methodName = __originalMethod.Name;

        // If we have a configured return value, use it
        if (interceptor.HasConfiguration(methodName, __args))
        {
            var result = interceptor.InterceptCall(methodName, __args);
            __result = result;
            return false; // Skip original method
        }

        // Otherwise, let the original method run
        return true;
    }

    /// <summary>
    /// Harmony prefix for void methods (no return value)
    /// </summary>
    private static bool VoidPrefixInterceptor(
        object __instance,
        MethodBase __originalMethod,
        object[] __args)
    {
        // Find the interceptor for this instance
        HarmonyMethodInterceptor interceptor = null;
        lock (_instanceInterceptors)
        {
            if (!_instanceInterceptors.TryGetValue(__instance, out interceptor))
            {
                // No interceptor configured, run original method
                return true;
            }
        }

        // Always record the call for verification
        interceptor.RecordCall(__originalMethod, __args);

        // Check if we have a configuration for this method
        var methodName = __originalMethod.Name;

        // If we have a configured return value, execute it
        if (interceptor.HasConfiguration(methodName, __args))
        {
            interceptor.InterceptCall(methodName, __args);
            return false; // Skip original method
        }

        // Otherwise, let the original method run
        return true;
    }

    public bool HasConfiguration(string methodName, object[] arguments)
    {
        // Check method configurations with arguments
        if (_methodConfigurations.TryGetValue(methodName, out var configs))
        {
            foreach (var config in configs)
            {
                if (ArgumentsMatch(config.Arguments, arguments))
                {
                    return true;
                }
            }
        }

        // Check simple method configurations
        if (_configuredReturns.ContainsKey(methodName))
        {
            return true;
        }

        // Check property configurations
        if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
        {
            var propertyName = methodName.Substring(4);
            return _propertyValues.ContainsKey(propertyName);
        }

        return false;
    }

    /// <summary>
    /// Record a method call for verification purposes
    /// </summary>
    public void RecordCall(MethodBase method, object[] arguments)
    {
        _receivedCalls.Add(new MethodCall
        {
            Method = method as MethodInfo,
            Arguments = arguments,
            Target = ProxyInstance
        });
    }

    public new IReadOnlyList<MethodCall> GetReceivedCalls()
    {
        return _receivedCalls.AsReadOnly();
    }

    public new int GetCallCount(MethodInfo method, object[] arguments)
    {
        return _receivedCalls.Count(c =>
            c.Method?.Name == method.Name && ArgumentsMatch(arguments, c.Arguments));
    }

    public void Unpatch()
    {
        _harmony.UnpatchAll(_harmony.Id);

        lock (_instanceInterceptors)
        {
            _instanceInterceptors.Remove(ProxyInstance);
        }

        _patchedMethods.Clear();
    }

    public override void Cleanup()
    {
        Unpatch();
        base.Cleanup();
    }
}