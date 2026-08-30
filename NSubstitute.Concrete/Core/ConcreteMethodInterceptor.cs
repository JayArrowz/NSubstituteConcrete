using NSubstitute.Core.Arguments;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.Threading.Tasks;
using NSubstitute.Concrete.Utilities;
using NSubstitute.Concrete.Callbacks;

namespace NSubstitute.Concrete.Core;

/// <summary>
/// Interceptor that routes method calls through our custom logic with callback support
/// </summary>
public class ConcreteMethodInterceptor
{
    // For methods configured without specific arguments. Keyed by MethodKeys, so two overloads of
    // the same name never share a bucket.
    protected readonly Dictionary<string, object> _configuredReturns = new Dictionary<string, object>();

    // For methods configured with specific arguments - now supports multiple configurations per method
    protected readonly Dictionary<string, List<(object[] Arguments, object ReturnValue)>> _methodConfigurations =
        new Dictionary<string, List<(object[], object)>>();

    // For property values
    protected readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

    protected readonly List<MethodCall> _receivedCalls = new List<MethodCall>();
    private object _proxy;
    private InterceptionFallback _fallback;

    public void SetProxy(object proxy)
    {
        _proxy = proxy;
    }

    /// <summary>The instance this interceptor stands in for, or <c>null</c> once it is cleaned up.</summary>
    protected object Proxy => _proxy;

    /// <summary>
    /// Installs the hook consulted when nothing configured here answers the call. See
    /// <see cref="InterceptionFallback"/>. Pass <c>null</c> to remove it.
    /// </summary>
    public void SetFallback(InterceptionFallback fallback)
    {
        _fallback = fallback;
    }

    /// <summary>Whether a fallback hook is installed.</summary>
    public bool HasFallback => _fallback != null;

    /// <summary>
    /// Runs the fallback hook, or returns <see cref="Interception.RunOriginal"/> when there is none.
    /// </summary>
    protected object RunFallback(MethodBase method, object instance, object[] arguments)
    {
        var fallback = _fallback;
        return fallback == null ? Interception.RunOriginal : fallback(method, instance, arguments);
    }

    /// <summary>
    /// Answers a call against the configuration held here, keyed by the method's full signature.
    /// Returns <see cref="Interception.RunOriginal"/> when nothing matched and no fallback answered.
    /// </summary>
    public object InterceptCall(MethodBase method, object[] arguments)
    {
        var key = MethodKeys.For(method);
        var name = method?.Name ?? string.Empty;

        _receivedCalls.Add(new MethodCall
        {
            Method = method as MethodInfo,
            Arguments = arguments,
            Target = _proxy,
            CalledAt = DateTime.UtcNow,
            Ordinal = Interception.NextOrdinal(),
        });

        return Answer(key, name, method, arguments);
    }

    /// <summary>
    /// Answers a call identified by name alone. Kept for callers that never had a
    /// <see cref="MethodBase"/> to hand; prefer <see cref="InterceptCall(MethodBase, object[])"/>,
    /// which can tell overloads apart.
    /// </summary>
    public object InterceptCall(string methodName, object[] arguments)
    {
        _receivedCalls.Add(new MethodCall
        {
            Method = null,
            Arguments = arguments,
            Target = _proxy,
            CalledAt = DateTime.UtcNow,
            Ordinal = Interception.NextOrdinal(),
        });

        var result = Answer(MethodKeys.ForName(methodName), methodName, null, arguments);
        return ReferenceEquals(result, Interception.RunOriginal)
            ? CallBaseMethod(methodName, arguments)
            : result;
    }

    private object Answer(string key, string methodName, MethodBase method, object[] arguments)
    {
        if (methodName.StartsWith("get_"))
        {
            var propertyName = methodName.Substring(4);
            if (_propertyValues.TryGetValue(propertyName, out var propertyValue))
            {
                return propertyValue;
            }
        }

        if (methodName.StartsWith("set_"))
        {
            var propertyName = methodName.Substring(4);
            if (arguments != null && arguments.Length == 1 && _propertyValues.ContainsKey(propertyName))
            {
                _propertyValues[propertyName] = arguments[0];
                return null; // Setters return void
            }
        }

        foreach (var candidate in Keys(key, methodName))
        {
            if (!_methodConfigurations.TryGetValue(candidate, out var configs)) continue;

            foreach (var config in configs)
            {
                if (!ArgumentsMatch(config.Arguments, arguments)) continue;

                var returnValue = config.ReturnValue;

                if (returnValue is ICallbackWrapper wrapper)
                {
                    return wrapper.Execute(arguments);
                }

                if (IsAsyncMethod(method, methodName) && returnValue != null && !IsTaskType(returnValue.GetType()))
                {
                    return WrapInTask(returnValue, GetMethodReturnType(method, methodName));
                }

                return returnValue;
            }
        }

        foreach (var candidate in Keys(key, methodName))
        {
            if (!_configuredReturns.TryGetValue(candidate, out var configuredReturn)) continue;

            if (configuredReturn is ICallbackWrapper wrapper)
            {
                return wrapper.Execute(arguments);
            }

            if (IsAsyncMethod(method, methodName) && configuredReturn != null && !IsTaskType(configuredReturn.GetType()))
            {
                return WrapInTask(configuredReturn, GetMethodReturnType(method, methodName));
            }

            return configuredReturn;
        }

        return RunFallback(method, _proxy, arguments);
    }

    /// <summary>
    /// The signature key first, then the name-only key, so a name-based configuration acts as a
    /// fallback for every overload rather than shadowing the precise one.
    /// </summary>
    private static IEnumerable<string> Keys(string key, string methodName)
    {
        if (!string.IsNullOrEmpty(key)) yield return key;
        yield return MethodKeys.ForName(methodName);
    }

    /// <summary>Whether anything configured here can answer this call.</summary>
    public bool HasConfiguration(MethodBase method, object[] arguments)
        => HasConfiguration(MethodKeys.For(method), method?.Name ?? string.Empty, arguments);

    /// <summary>Whether anything configured here can answer a call to a method with this name.</summary>
    public bool HasConfiguration(string methodName, object[] arguments)
        => HasConfiguration(MethodKeys.ForName(methodName), methodName, arguments);

    private bool HasConfiguration(string key, string methodName, object[] arguments)
    {
        foreach (var candidate in Keys(key, methodName))
        {
            if (_methodConfigurations.TryGetValue(candidate, out var configs)
                && configs.Any(config => ArgumentsMatch(config.Arguments, arguments)))
            {
                return true;
            }

            if (_configuredReturns.ContainsKey(candidate)) return true;
        }

        if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
        {
            var propertyName = methodName.Substring(4);
            return _propertyValues.ContainsKey(propertyName);
        }

        return false;
    }

    public bool IsAsyncMethod(string methodName) => IsAsyncMethod(null, methodName);

    private bool IsAsyncMethod(MethodBase method, string methodName)
    {
        if (method is MethodInfo known) return IsTaskType(known.ReturnType);
        if (_proxy == null) return false;

        var resolved = _proxy.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return resolved != null && IsTaskType(resolved.ReturnType);
    }

    private Type GetMethodReturnType(MethodBase method, string methodName)
    {
        if (method is MethodInfo known) return known.ReturnType;
        if (_proxy == null) return typeof(object);

        var resolved = _proxy.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return resolved?.ReturnType ?? typeof(object);
    }

    private bool IsTaskType(Type type)
    {
        return type == typeof(Task) ||
               type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Task<>);
    }

    private object WrapInTask(object value, Type expectedReturnType)
    {
        if (expectedReturnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (expectedReturnType.IsGenericType && expectedReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = expectedReturnType.GetGenericArguments()[0];
            var fromResultMethod = typeof(Task).GetMethod("FromResult").MakeGenericMethod(taskResultType);
            return fromResultMethod.Invoke(null, new[] { value });
        }

        return value;
    }

    private object CallBaseMethod(string methodName, object[] arguments)
    {
        if (_proxy == null) return GetDefaultValue(typeof(object));
        var baseType = _proxy.GetType().BaseType;
        if (baseType == null) return GetDefaultValue(typeof(object));

        try
        {
            var parameterTypes = arguments?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? new Type[0];
            var baseMethod = baseType.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic,
                null, parameterTypes, null);

            if (baseMethod == null)
            {
                baseMethod = baseType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (baseMethod != null)
            {
                return baseMethod.Invoke(_proxy, arguments);
            }
        }
        catch (Exception)
        {
            // If calling the base method fails, return default value
        }

        return GetDefaultValue(typeof(object));
    }

    private static object GetDefaultValue(Type type)
    {
        if (type == typeof(void))
            return null;
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        return null;
    }

    public void ConfigureReturn(string methodName, object returnValue)
    {
        _configuredReturns[MethodKeys.ForName(methodName)] = returnValue;
    }

    public void ConfigureReturn(MethodInfo method, object[] arguments, object returnValue)
    {
        var key = MethodKeys.For(method);
        if (!_methodConfigurations.TryGetValue(key, out var configs))
        {
            configs = new List<(object[], object)>();
            _methodConfigurations[key] = configs;
        }

        configs.Add((arguments, returnValue));
    }

    public void ConfigureProperty(string propertyName, object value)
    {
        _propertyValues[propertyName] = value;
    }

    protected bool ArgumentsMatch(object[] setupArgs, object[] callArgs)
    {
        if (setupArgs == null && callArgs == null) return true;
        if (setupArgs == null || callArgs == null) return false;
        if (setupArgs.Length != callArgs.Length) return false;

        for (int i = 0; i < setupArgs.Length; i++)
        {
            if (setupArgs[i] is IArgumentMatcher matcher)
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

    public virtual IReadOnlyList<MethodCall> GetReceivedCalls()
    {
        return _receivedCalls.AsReadOnly();
    }

    public int GetCallCount(string methodName)
    {
        return _receivedCalls.Count(c => c.Method != null && c.Method.Name == methodName);
    }

    /// <summary>
    /// Calls to <paramref name="method"/> whose arguments satisfy <paramref name="arguments"/>.
    /// Calls to a different method never count, however their arguments compare.
    /// </summary>
    public virtual int GetCallCount(MethodInfo method, object[] arguments)
    {
        var key = MethodKeys.For(method);
        return _receivedCalls.Count(c => MethodKeys.For(c.Method) == key && ArgumentsMatch(arguments, c.Arguments));
    }

    /// <summary>
    /// Clear all internal state to help with garbage collection
    /// </summary>
    public virtual void Cleanup()
    {
        _configuredReturns.Clear();
        _methodConfigurations.Clear();
        _propertyValues.Clear();
        _receivedCalls.Clear();
        _fallback = null;
        _proxy = null; // Release reference to proxy
    }
}
