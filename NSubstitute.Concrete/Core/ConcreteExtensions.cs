using NSubstitute.Concrete.Setup.Instance;
using NSubstitute.Concrete.Setup.Interfaces;
using NSubstitute.Core.Arguments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Core;

/// <summary>
/// Extension methods to provide NSubstitute-like syntax for concrete substitutes with auto-patching
/// </summary>
public static class ConcreteExtensions
{
    private static readonly ConcurrentDictionary<object, ConcreteMethodInterceptor> _interceptors
        = new ConcurrentDictionary<object, ConcreteMethodInterceptor>();

    /// <summary>
    /// Configure method to return a specific value using expression with auto-patching
    /// </summary>
    public static IMethodSetup<T, TResult> Setup<T, TResult>(this T substitute, Expression<Func<T, TResult>> expression) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var methodInfo = GetMethodFromExpression(expression);
            var arguments = ExtractArgumentsFromExpression(expression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchMethod(methodInfo);
            }

            return new MethodSetup<T, TResult>(interceptor, methodInfo, arguments);
        }

        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Configure async method to return a specific value with auto-patching
    /// </summary>
    public static IAsyncMethodSetup<T, TResult> SetupAsync<T, TResult>(this T substitute, Expression<Func<T, Task<TResult>>> expression) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var methodInfo = GetMethodFromExpression(expression);
            var arguments = ExtractArgumentsFromExpression(expression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchMethod(methodInfo);
            }

            return new AsyncMethodSetup<T, TResult>(interceptor, methodInfo, arguments);
        }

        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Configure async void method with auto-patching
    /// </summary>
    public static IVoidAsyncMethodSetup<T> SetupAsync<T>(this T substitute, Expression<Func<T, Task>> expression) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var methodInfo = GetMethodFromExpression(expression);
            var arguments = ExtractArgumentsFromExpression(expression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchMethod(methodInfo);
            }

            return new VoidAsyncMethodSetup<T>(interceptor, methodInfo, arguments);
        }

        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Configure a property to return a specific value with auto-patching
    /// </summary>
    public static IPropertySetup<T, TResult> SetupProperty<T, TResult>(this T substitute, Expression<Func<T, TResult>> propertyExpression) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var propertyInfo = GetPropertyFromExpression(propertyExpression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchProperty(propertyInfo);
            }

            return new PropertySetup<T, TResult>(interceptor, propertyInfo);
        }

        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Configure void method with auto-patching
    /// </summary>
    public static IVoidMethodSetup<T> Setup<T>(this T substitute, Expression<Action<T>> expression)
where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var methodInfo = GetMethodFromExpressionAction(expression);
            var arguments = ExtractActualArgumentsFromExpressionAction(expression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchMethod(methodInfo);
            }

            return new VoidMethodSetup<T>(interceptor, methodInfo, arguments);
        }
        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Set a property value directly with auto-patching
    /// </summary>
    public static T SetProperty<T, TValue>(this T substitute, Expression<Func<T, TValue>> propertyExpression, TValue value) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var propertyInfo = GetPropertyFromExpression(propertyExpression);

            // Auto-patch if using Harmony interceptor
            if (interceptor is HarmonyMethodInterceptor harmonyInterceptor)
            {
                harmonyInterceptor.PatchProperty(propertyInfo);
            }

            interceptor.ConfigureProperty(propertyInfo.Name, value);

            try
            {
                propertyInfo.SetValue(substitute, value);
            }
            catch
            {
                // If setting fails, the interceptor setup should still work
            }

            return substitute;
        }

        throw new InvalidOperationException("Could not find interceptor for substitute");
    }

    /// <summary>
    /// Verify that a method was called using expression
    /// </summary>
    public static void Verify<T, TResult>(this T substitute, Expression<Func<T, TResult>> expression, int times = 1) where T : class
    {
        if (TryGetInterceptor(substitute, out var interceptor))
        {
            var methodInfo = GetMethodFromExpression(expression);
            var arguments = ExtractArgumentsFromExpression(expression);
            var actualCalls = interceptor.GetCallCount(methodInfo, arguments);
            if (actualCalls != times)
            {
                throw new Exception($"Expected {times} calls to {methodInfo.Name}, but received {actualCalls}");
            }
        }
    }

    /// <summary>
    /// Call a method on the substitute using expression to ensure interception works
    /// </summary>
    public static TResult Call<T, TResult>(this T substitute, Expression<Func<T, TResult>> expression) where T : class
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractActualArgumentsFromExpression(expression);

        try
        {
            var result = methodInfo.Invoke(substitute, arguments);
            return (TResult)result;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Call a void method on the substitute using expression to ensure interception works
    /// </summary>
    public static void Call<T>(this T substitute, Expression<Action<T>> expression) where T : class
    {
        var methodInfo = GetMethodFromExpressionAction(expression);
        var arguments = ExtractActualArgumentsFromExpressionAction(expression);

        try
        {
            methodInfo.Invoke(substitute, arguments);
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Get a property value (convenience method)
    /// </summary>
    public static TResult GetProperty<T, TResult>(this T substitute, Expression<Func<T, TResult>> propertyExpression) where T : class
    {
        return substitute.Call(propertyExpression);
    }

    #region Helper Methods

    private static object[] ExtractArgumentsFromExpression<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
        {
            var arguments = new object[methodCall.Arguments.Count];
            for (int i = 0; i < methodCall.Arguments.Count; i++)
            {
                var arg = methodCall.Arguments[i];

                if (arg is ConstantExpression constant)
                {
                    arguments[i] = constant.Value;
                }
                else if (arg is MethodCallExpression itMethodCall &&
                         itMethodCall.Method.DeclaringType?.Name == "It" &&
                         itMethodCall.Method.Name == "IsAny")
                {
                    var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                    arguments[i] = new AnyArgumentMatcher(parameterType);
                }
                else
                {
                    try
                    {
                        var lambda = Expression.Lambda(arg);
                        var compiled = lambda.Compile();
                        arguments[i] = compiled.DynamicInvoke();
                    }
                    catch
                    {
                        var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                        arguments[i] = new AnyArgumentMatcher(parameterType);
                    }
                }
            }
            return arguments;
        }

        return new object[0];
    }

    private static object[] ExtractActualArgumentsFromExpression<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
        {
            var arguments = new object[methodCall.Arguments.Count];
            for (int i = 0; i < methodCall.Arguments.Count; i++)
            {
                var arg = methodCall.Arguments[i];
                if (arg is ConstantExpression constant)
                {
                    arguments[i] = constant.Value;
                }
                else if (arg is MethodCallExpression { Method.DeclaringType.Name: "It" })
                {
                    var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                    arguments[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
                }
                else
                {
                    try
                    {
                        var lambda = Expression.Lambda(arg);
                        var compiled = lambda.Compile();
                        arguments[i] = compiled.DynamicInvoke();
                    }
                    catch
                    {
                        var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                        arguments[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
                    }
                }
            }
            return arguments;
        }

        return new object[0];
    }

    private static object[] ExtractActualArgumentsFromExpressionAction<T>(Expression<Action<T>> expression)
    {
        if (expression.Body is MethodCallExpression methodCall)
        {
            var arguments = new object[methodCall.Arguments.Count];
            for (int i = 0; i < methodCall.Arguments.Count; i++)
            {
                var arg = methodCall.Arguments[i];

                // Handle It.IsAny (matcher)
                if (arg is MethodCallExpression itMethodCall &&
                         itMethodCall.Method.DeclaringType?.Name == "It" &&
                         itMethodCall.Method.Name == "IsAny")
                {
                    var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                    arguments[i] = new AnyArgumentMatcher(parameterType);
                }
                else if (arg is ConstantExpression constant)
                {
                    arguments[i] = constant.Value;
                }
                else
                {
                    try
                    {
                        var lambda = Expression.Lambda(arg);
                        var compiled = lambda.Compile();
                        arguments[i] = compiled.DynamicInvoke();
                    }
                    catch
                    {
                        var parameterType = methodCall.Method.GetParameters()[i].ParameterType;
                        arguments[i] = parameterType.IsValueType
                            ? Activator.CreateInstance(parameterType)
                            : null;
                    }
                }
            }
            return arguments;
        }

        return new object[0];
    }

    private static MethodInfo GetMethodFromExpression<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        return expression.Body switch
        {
            MethodCallExpression methodCall => methodCall.Method,
            MemberExpression { Member: PropertyInfo property } => property.GetGetMethod(),
            _ => throw new ArgumentException("Expression must be a method call or property access")
        };
    }

    private static PropertyInfo GetPropertyFromExpression<T, TResult>(Expression<Func<T, TResult>> expression)
    {
        return expression.Body switch
        {
            MemberExpression { Member: PropertyInfo property } => property,
            _ => throw new ArgumentException("Expression must be a property access")
        };
    }

    private static MethodInfo GetMethodFromExpressionAction<T>(Expression<Action<T>> expression)
    {
        return expression.Body switch
        {
            MethodCallExpression methodCall => methodCall.Method,
            _ => throw new ArgumentException("Expression must be a method call")
        };
    }

    private static bool TryGetInterceptor<T>(T substitute, out ConcreteMethodInterceptor interceptor) where T : class
    {
        var found = _interceptors.TryGetValue(substitute, out interceptor);
        return found;
    }

    #endregion

    #region Internal Registry Management

    internal static void RegisterInterceptor(object substitute, ConcreteMethodInterceptor interceptor)
    {
        _interceptors[substitute] = interceptor;
    }

    /// <summary>
    /// Remove a specific substitute from the registry
    /// </summary>
    internal static void UnregisterInterceptor(object substitute)
    {
        if (_interceptors.TryRemove(substitute, out var interceptor))
        {
            interceptor.Cleanup();
        }

        // Also cleanup from Harmony registry if applicable
        NSubstituteExtensions.UnregisterInterceptor(substitute);
    }

    /// <summary>
    /// Clear all registered interceptors
    /// </summary>
    internal static void ClearAllInterceptors()
    {
        foreach (var interceptor in _interceptors.Values)
        {
            interceptor.Cleanup();
        }
        _interceptors.Clear();

        // Also clear Harmony registry
        NSubstituteExtensions.ClearAllInterceptors();
    }

    /// <summary>
    /// Get the count of registered interceptors
    /// </summary>
    internal static int GetInterceptorCount()
    {
        return _interceptors.Count;
    }

    #endregion
}