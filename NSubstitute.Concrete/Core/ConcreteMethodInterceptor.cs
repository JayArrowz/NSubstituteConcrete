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
    // For methods configured without specific arguments (just method name)
    protected readonly Dictionary<string, object> _configuredReturns = new Dictionary<string, object>();

    // For methods configured with specific arguments - now supports multiple configurations per method
    protected readonly Dictionary<string, List<(object[] Arguments, object ReturnValue)>> _methodConfigurations =
        new Dictionary<string, List<(object[], object)>>();

    // For property values
    protected readonly Dictionary<string, object> _propertyValues = new Dictionary<string, object>();

    protected readonly List<MethodCall> _receivedCalls = new List<MethodCall>();
    private object _proxy;

    public void SetProxy(object proxy)
    {
        _proxy = proxy;
    }

    public object InterceptCall(string methodName, object[] arguments)
    {
        // Record the call
        _receivedCalls.Add(new MethodCall
        {
            Method = null,
            Arguments = arguments,
            Target = _proxy
        });

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
            if (arguments != null && arguments.Length == 1)
            {
                _propertyValues[propertyName] = arguments[0];
                return null; // Setters return void
            }
        }

        if (_methodConfigurations.TryGetValue(methodName, out var configs))
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

                    if (IsAsyncMethod(methodName) && returnValue != null && !IsTaskType(returnValue.GetType()))
                    {
                        return WrapInTask(returnValue, GetMethodReturnType(methodName));
                    }

                    return returnValue;
                }
            }
            return CallBaseMethod(methodName, arguments);
        }

        if (_configuredReturns.TryGetValue(methodName, out var configuredReturn))
        {
            if (configuredReturn is ICallbackWrapper wrapper)
            {
                return wrapper.Execute(arguments);
            }

            if (IsAsyncMethod(methodName) && configuredReturn != null && !IsTaskType(configuredReturn.GetType()))
            {
                return WrapInTask(configuredReturn, GetMethodReturnType(methodName));
            }

            return configuredReturn;
        }

        return CallBaseMethod(methodName, arguments);
    }

    public bool IsAsyncMethod(string methodName)
    {
        if (_proxy == null) return false;

        var method = _proxy.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return method != null && IsTaskType(method.ReturnType);
    }

    private Type GetMethodReturnType(string methodName)
    {
        if (_proxy == null) return typeof(object);

        var method = _proxy.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        return method?.ReturnType ?? typeof(object);
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
        _configuredReturns[methodName] = returnValue;
    }

    public void ConfigureReturn(MethodInfo method, object[] arguments, object returnValue)
    {
        var methodName = method.Name;
        if (!_methodConfigurations.TryGetValue(methodName, out var configs))
        {
            configs = new List<(object[], object)>();
            _methodConfigurations[methodName] = configs;
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

    public IReadOnlyList<MethodCall> GetReceivedCalls()
    {
        return _receivedCalls.AsReadOnly();
    }

    public int GetCallCount(string methodName)
    {
        return _receivedCalls.Count(c => c.Arguments != null);
    }

    public int GetCallCount(MethodInfo method, object[] arguments)
    {
        return _receivedCalls.Count(c => ArgumentsMatch(arguments, c.Arguments));
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
        _proxy = null; // Release reference to proxy
    }
}