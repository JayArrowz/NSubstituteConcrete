using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace NSubstitute.Concrete;

/// <summary>
/// Global interceptor for static method substitution using Harmony
/// </summary>
public class StaticMethodInterceptor
{
    private static readonly StaticMethodInterceptor _instance = new StaticMethodInterceptor();
    public static StaticMethodInterceptor Instance => _instance;

    private readonly Harmony _harmony;
    internal readonly ConcurrentDictionary<string, object> _configuredReturns = new ConcurrentDictionary<string, object>();
    internal readonly ConcurrentDictionary<string, List<(object[] Arguments, object ReturnValue)>> _methodConfigurations = new ConcurrentDictionary<string, List<(object[], object)>>();
    private readonly ConcurrentDictionary<string, List<MethodCall>> _receivedCalls = new ConcurrentDictionary<string, List<MethodCall>>();
    internal readonly ConcurrentDictionary<MethodBase, bool> _patchedMethods = new ConcurrentDictionary<MethodBase, bool>();
    private readonly object _callLock = new object();

    private StaticMethodInterceptor()
    {
        _harmony = new Harmony("NSubstitute.Concrete.Static");
    }

    /// <summary>
    /// Patch a static method for interception
    /// </summary>
    public void PatchMethod(MethodInfo method)
    {
        if (!method.IsStatic)
            throw new ArgumentException("Method must be static", nameof(method));

        if (_patchedMethods.ContainsKey(method))
            return;

        try
        {
            // Choose the right prefix based on whether method returns void
            string prefixName = method.ReturnType == typeof(void)
                ? nameof(VoidPrefixInterceptor)
                : nameof(PrefixInterceptor);

            var prefix = typeof(StaticMethodInterceptor).GetMethod(
                prefixName,
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
            _patchedMethods[method] = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to patch static method {method.DeclaringType?.Name}.{method.Name}", ex);
        }
    }

    /// <summary>
    /// Configure a static method return value
    /// </summary>
    public void ConfigureReturn(MethodInfo method, object[] arguments, object returnValue)
    {
        var methodKey = GetMethodKey(method);

        if (arguments == null || arguments.Length == 0)
        {
            _configuredReturns[methodKey] = returnValue;
        }
        else
        {
            var configs = _methodConfigurations.GetOrAdd(methodKey, _ => new List<(object[], object)>());
            lock (configs)
            {
                configs.Add((arguments, returnValue));
            }
        }
    }

    /// <summary>
    /// Get call count for a static method
    /// </summary>
    public int GetCallCount(MethodInfo method, object[] arguments = null)
    {
        var methodKey = GetMethodKey(method);
        if (!_receivedCalls.TryGetValue(methodKey, out var calls))
            return 0;

        lock (calls)
        {
            if (arguments == null)
                return calls.Count;

            return calls.Count(c => ArgumentsMatch(arguments, c.Arguments));
        }
    }

    /// <summary>
    /// Get all calls for a static method
    /// </summary>
    public IReadOnlyList<MethodCall> GetCalls(MethodInfo method)
    {
        var methodKey = GetMethodKey(method);
        if (!_receivedCalls.TryGetValue(methodKey, out var calls))
            return new List<MethodCall>().AsReadOnly();

        lock (calls)
        {
            return calls.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Clear all static method configurations and patches
    /// </summary>
    public void ClearAll()
    {
        // Remove all Harmony patches
        _harmony.UnpatchAll(_harmony.Id);

        // Clear all state
        _configuredReturns.Clear();
        _methodConfigurations.Clear();
        _receivedCalls.Clear();
        _patchedMethods.Clear();
    }

    /// <summary>
    /// Harmony prefix interceptor for static methods with return values
    /// </summary>
    private static bool PrefixInterceptor(
        MethodBase __originalMethod,
        object[] __args,
        ref object __result)
    {
        var instance = Instance;
        var methodKey = instance.GetMethodKey(__originalMethod as MethodInfo);

        // Always record the call
        instance.RecordCall(__originalMethod as MethodInfo, __args);

        // Check if we have a configuration for this method
        if (instance.HasConfiguration(methodKey, __args))
        {
            var result = instance.InterceptCall(methodKey, __args);
            __result = result;
            return false; // Skip original method
        }

        // Let the original method run
        return true;
    }

    /// <summary>
    /// Harmony prefix interceptor for void static methods
    /// </summary>
    private static bool VoidPrefixInterceptor(
        MethodBase __originalMethod,
        object[] __args)
    {
        var instance = Instance;
        var methodKey = instance.GetMethodKey(__originalMethod as MethodInfo);

        // Always record the call
        instance.RecordCall(__originalMethod as MethodInfo, __args);

        // Check if we have a configuration for this method
        if (instance.HasConfiguration(methodKey, __args))
        {
            instance.InterceptCall(methodKey, __args);
            return false; // Skip original method
        }

        // Let the original method run
        return true;
    }

    private void RecordCall(MethodInfo method, object[] arguments)
    {
        if (method == null) return;

        var methodKey = GetMethodKey(method);
        var calls = _receivedCalls.GetOrAdd(methodKey, _ => new List<MethodCall>());

        lock (calls)
        {
            calls.Add(new MethodCall
            {
                Method = method,
                Arguments = arguments,
                Target = null, // Static methods have no target
                CalledAt = DateTime.UtcNow
            });
        }
    }

    private bool HasConfiguration(string methodKey, object[] arguments)
    {
        // Check method configurations with arguments
        if (_methodConfigurations.TryGetValue(methodKey, out var configs))
        {
            lock (configs)
            {
                foreach (var config in configs)
                {
                    if (ArgumentsMatch(config.Arguments, arguments))
                    {
                        return true;
                    }
                }
            }
        }

        // Check simple method configurations
        return _configuredReturns.ContainsKey(methodKey);
    }

    private object InterceptCall(string methodKey, object[] arguments)
    {
        // Check method configurations with arguments first
        if (_methodConfigurations.TryGetValue(methodKey, out var configs))
        {
            lock (configs)
            {
                foreach (var config in configs)
                {
                    if (ArgumentsMatch(config.Arguments, arguments))
                    {
                        var returnValue = config.ReturnValue;

                        if (returnValue is ICallbackWrapper wrapper)
                        {
                            return wrapper.Execute(arguments);
                        }

                        return returnValue;
                    }
                }
            }
        }

        // Check simple method configurations
        if (_configuredReturns.TryGetValue(methodKey, out var configuredReturn))
        {
            if (configuredReturn is ICallbackWrapper wrapper)
            {
                return wrapper.Execute(arguments);
            }

            return configuredReturn;
        }

        return null;
    }

    private bool ArgumentsMatch(object[] setupArgs, object[] callArgs)
    {
        if (setupArgs == null && callArgs == null) return true;
        if (setupArgs == null || callArgs == null) return false;
        if (setupArgs.Length != callArgs.Length) return false;

        for (int i = 0; i < setupArgs.Length; i++)
        {
            if (setupArgs[i] is NSubstitute.Core.Arguments.IArgumentMatcher matcher)
            {
                if (!matcher.IsSatisfiedBy(callArgs[i]))
                {
                    return false;
                }
            }
            else if (!Equals(setupArgs[i], callArgs[i]))
            {
                return false;
            }
        }
        return true;
    }

    private string GetMethodKey(MethodInfo method)
    {
        if (method == null) return "";

        var parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
        return $"{method.DeclaringType?.FullName}.{method.Name}({parameters})";
    }
}