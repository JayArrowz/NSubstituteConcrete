using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Entry point for static method substitution
/// </summary>
public static class Static
{
    /// <summary>
    /// Configure a static method to return a specific value
    /// </summary>
    /// <typeparam name="TResult">Return type of the method</typeparam>
    /// <param name="expression">Expression representing the static method call</param>
    /// <returns>Setup object for further configuration</returns>
    public static IStaticMethodSetup<TResult> Setup<TResult>(Expression<Func<TResult>> expression)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        // Patch the method
        StaticMethodInterceptor.Instance.PatchMethod(methodInfo);

        return new StaticMethodSetup<TResult>(methodInfo, arguments);
    }

    /// <summary>
    /// Configure a static void method
    /// </summary>
    /// <param name="expression">Expression representing the static method call</param>
    /// <returns>Setup object for further configuration</returns>
    public static IStaticVoidMethodSetup Setup(Expression<Action> expression)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        // Patch the method
        StaticMethodInterceptor.Instance.PatchMethod(methodInfo);

        return new StaticVoidMethodSetup(methodInfo, arguments);
    }

    /// <summary>
    /// Configure a static async method
    /// </summary>
    /// <typeparam name="TResult">Return type of the async method</typeparam>
    /// <param name="expression">Expression representing the static async method call</param>
    /// <returns>Setup object for further configuration</returns>
    public static IStaticAsyncMethodSetup<TResult> SetupAsync<TResult>(Expression<Func<Task<TResult>>> expression)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        // Patch the method
        StaticMethodInterceptor.Instance.PatchMethod(methodInfo);

        return new StaticAsyncMethodSetup<TResult>(methodInfo, arguments);
    }

    /// <summary>
    /// Configure a static async void method
    /// </summary>
    /// <param name="expression">Expression representing the static async method call</param>
    /// <returns>Setup object for further configuration</returns>
    public static IStaticVoidAsyncMethodSetup SetupAsync(Expression<Func<Task>> expression)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        // Patch the method
        StaticMethodInterceptor.Instance.PatchMethod(methodInfo);

        return new StaticVoidAsyncMethodSetup(methodInfo, arguments);
    }

    /// <summary>
    /// Verify that a static method was called
    /// </summary>
    /// <typeparam name="TResult">Return type of the method</typeparam>
    /// <param name="expression">Expression representing the static method call</param>
    /// <param name="times">Expected number of calls</param>
    public static void Verify<TResult>(Expression<Func<TResult>> expression, int times = 1)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        var actualCalls = StaticMethodInterceptor.Instance.GetCallCount(methodInfo, arguments);
        if (actualCalls != times)
        {
            throw new Exception($"Expected {times} calls to {methodInfo.DeclaringType?.Name}.{methodInfo.Name}, but received {actualCalls}");
        }
    }

    /// <summary>
    /// Verify that a static void method was called
    /// </summary>
    /// <param name="expression">Expression representing the static method call</param>
    /// <param name="times">Expected number of calls</param>
    public static void Verify(Expression<Action> expression, int times = 1)
    {
        var methodInfo = GetMethodFromExpression(expression);
        var arguments = ExtractArgumentsFromExpression(expression);

        var actualCalls = StaticMethodInterceptor.Instance.GetCallCount(methodInfo, arguments);
        if (actualCalls != times)
        {
            throw new Exception($"Expected {times} calls to {methodInfo.DeclaringType?.Name}.{methodInfo.Name}, but received {actualCalls}");
        }
    }

    /// <summary>
    /// Clear all static method configurations and patches
    /// Important: Call this in test cleanup to prevent interference between tests
    /// </summary>
    public static void ClearAll()
    {
        StaticMethodInterceptor.Instance.ClearAll();
    }

    /// <summary>
    /// Get diagnostic information about static method substitutions
    /// </summary>
    public static StaticDiagnostics GetDiagnostics()
    {
        return new StaticDiagnostics
        {
            PatchedMethodCount = StaticMethodInterceptor.Instance._patchedMethods.Count,
            ConfiguredMethodCount = StaticMethodInterceptor.Instance._configuredReturns.Count +
                                  StaticMethodInterceptor.Instance._methodConfigurations.Count
        };
    }

    #region Helper Methods

    private static MethodInfo GetMethodFromExpression<TResult>(Expression<Func<TResult>> expression)
    {
        return expression.Body switch
        {
            MethodCallExpression methodCall => methodCall.Method,
            _ => throw new ArgumentException("Expression must be a static method call")
        };
    }

    private static MethodInfo GetMethodFromExpression(Expression<Action> expression)
    {
        return expression.Body switch
        {
            MethodCallExpression methodCall => methodCall.Method,
            _ => throw new ArgumentException("Expression must be a static method call")
        };
    }

    private static object[] ExtractArgumentsFromExpression<TResult>(Expression<Func<TResult>> expression)
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
                    arguments[i] = new NSubstitute.Core.Arguments.AnyArgumentMatcher(parameterType);
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
                        arguments[i] = new NSubstitute.Core.Arguments.AnyArgumentMatcher(parameterType);
                    }
                }
            }
            return arguments;
        }

        return new object[0];
    }

    private static object[] ExtractArgumentsFromExpression(Expression<Action> expression)
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
                    arguments[i] = new NSubstitute.Core.Arguments.AnyArgumentMatcher(parameterType);
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
                        arguments[i] = new NSubstitute.Core.Arguments.AnyArgumentMatcher(parameterType);
                    }
                }
            }
            return arguments;
        }

        return new object[0];
    }

    #endregion
}

/// <summary>
/// Diagnostic information for static method substitutions
/// </summary>
public class StaticDiagnostics
{
    public int PatchedMethodCount { get; set; }
    public int ConfiguredMethodCount { get; set; }

    public override string ToString()
    {
        return $"Patched Methods: {PatchedMethodCount}, Configured Methods: {ConfiguredMethodCount}";
    }
}