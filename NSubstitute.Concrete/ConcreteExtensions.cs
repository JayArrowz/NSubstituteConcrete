using NSubstitute.Core.Arguments;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete
{
    /// <summary>
    /// Extension methods to provide NSubstitute-like syntax for concrete substitutes
    /// </summary>
    public static class ConcreteExtensions
    {
        private static readonly ConcurrentDictionary<object, ConcreteMethodInterceptor> _interceptors
            = new ConcurrentDictionary<object, ConcreteMethodInterceptor>();

        private static readonly ConcurrentDictionary<object, Type> _substituteTypes
            = new ConcurrentDictionary<object, Type>();

        /// <summary>
        /// Configure method to return a specific value using expression
        /// </summary>
        public static IMethodSetup<T, TResult> Setup<T, TResult>(this T substitute, Expression<Func<T, TResult>> expression) where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var methodInfo = GetMethodFromExpression(expression);
                var arguments = ExtractArgumentsFromExpression(expression);
                return new MethodSetup<T, TResult>(interceptor, methodInfo, arguments);
            }

            throw new InvalidOperationException("Could not find interceptor for substitute");
        }

        /// <summary>
        /// Configure async method to return a specific value (auto-wraps in Task.FromResult)
        /// </summary>
        public static IAsyncMethodSetup<T, TResult> SetupAsync<T, TResult>(this T substitute, Expression<Func<T, Task<TResult>>> expression) where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var methodInfo = GetMethodFromExpression(expression);
                var arguments = ExtractArgumentsFromExpression(expression);
                return new AsyncMethodSetup<T, TResult>(interceptor, methodInfo, arguments);
            }

            throw new InvalidOperationException("Could not find interceptor for substitute");
        }

        /// <summary>
        /// Configure async void method (Task without result)
        /// </summary>
        public static IVoidAsyncMethodSetup<T> SetupAsync<T>(this T substitute, Expression<Func<T, Task>> expression) where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var methodInfo = GetMethodFromExpression(expression);
                var arguments = ExtractArgumentsFromExpression(expression);
                return new VoidAsyncMethodSetup<T>(interceptor, methodInfo, arguments);
            }

            throw new InvalidOperationException("Could not find interceptor for substitute");
        }

        /// <summary>
        /// Configure a property to return a specific value
        /// </summary>
        public static IPropertySetup<T, TResult> SetupProperty<T, TResult>(this T substitute, Expression<Func<T, TResult>> propertyExpression) where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var propertyInfo = GetPropertyFromExpression(propertyExpression);
                return new PropertySetup<T, TResult>(interceptor, propertyInfo);
            }

            throw new InvalidOperationException("Could not find interceptor for substitute");
        }

        public static IVoidMethodSetup<T> Setup<T>(this T substitute, Expression<Action<T>> expression)
    where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var methodInfo = GetMethodFromExpressionAction(expression);
                var arguments = ExtractActualArgumentsFromExpressionAction(expression);
                return new VoidMethodSetup<T>(interceptor, methodInfo, arguments);
            }
            throw new InvalidOperationException("Could not find interceptor for substitute");
        }

        /// <summary>
        /// Set a property value directly
        /// </summary>
        public static T SetProperty<T, TValue>(this T substitute, Expression<Func<T, TValue>> propertyExpression, TValue value) where T : class
        {
            if (TryGetInterceptor(substitute, out var interceptor))
            {
                var propertyInfo = GetPropertyFromExpression(propertyExpression);
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

        private static bool TryGetInterceptor<T>(T substitute, out ConcreteMethodInterceptor interceptor) where T : class
        {
            var found = _interceptors.TryGetValue(substitute, out interceptor);
            return found;
        }

        internal static void RegisterInterceptor(object substitute, ConcreteMethodInterceptor interceptor)
        {
            _interceptors[substitute] = interceptor;
            var originalType = substitute.GetType().BaseType;
            if (originalType != null)
            {
                _substituteTypes[substitute] = originalType;
            }
        }

        /// <summary>
        /// Call a method on the substitute using expression to ensure interception works (no strings!)
        /// </summary>
        public static TResult Call<T, TResult>(this T substitute, Expression<Func<T, TResult>> expression) where T : class
        {
            var methodInfo = GetMethodFromExpression(expression);
            var arguments = ExtractActualArgumentsFromExpression(expression);
            var parameterTypes = arguments.Select(a => a?.GetType() ?? typeof(object)).ToArray();
            var proxyMethod = substitute.GetType().GetMethod(methodInfo.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, parameterTypes, null);
            var methodToCall = proxyMethod ?? methodInfo;

            try
            {
                var result = methodToCall.Invoke(substitute, arguments);
                return (TResult)result;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Call a void method on the substitute using expression to ensure interception works (no strings!)
        /// </summary>
        public static void Call<T>(this T substitute, Expression<Action<T>> expression) where T : class
        {
            var methodInfo = GetMethodFromExpressionAction(expression);
            var arguments = ExtractActualArgumentsFromExpressionAction(expression);
            var parameterTypes = arguments.Select(a => a?.GetType() ?? typeof(object)).ToArray();
            var proxyMethod = substitute.GetType().GetMethod(methodInfo.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, parameterTypes, null);
            var methodToCall = proxyMethod ?? methodInfo;

            try
            {
                methodToCall.Invoke(substitute, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
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

        private static MethodInfo GetMethodFromExpressionAction<T>(Expression<Action<T>> expression)
        {
            return expression.Body switch
            {
                MethodCallExpression methodCall => methodCall.Method,
                _ => throw new ArgumentException("Expression must be a method call")
            };
        }

        /// <summary>
        /// Call a method on the substitute using reflection to ensure interception works
        /// </summary>
        public static TResult CallMethod<T, TResult>(this T substitute, string methodName, params object[] arguments) where T : class
        {
            var method = substitute.GetType().GetMethod(methodName);
            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found on type {substitute.GetType().Name}");
            }

            try
            {
                var result = method.Invoke(substitute, arguments);
                return (TResult)result;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }


        /// <summary>
        /// Call a void method on the substitute using reflection to ensure interception works
        /// </summary>
        public static void CallMethod<T>(this T substitute, string methodName, params object[] arguments) where T : class
        {
            var method = substitute.GetType().GetMethod(methodName);
            if (method == null)
            {
                throw new ArgumentException($"Method {methodName} not found on type {substitute.GetType().Name}");
            }

            try
            {
                method.Invoke(substitute, arguments);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Get a property value (convenience method for non-virtual properties)
        /// </summary>
        public static TResult GetProperty<T, TResult>(this T substitute, Expression<Func<T, TResult>> propertyExpression) where T : class
        {
            return substitute.Call(propertyExpression);
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

            if (_substituteTypes.TryRemove(substitute, out var originalType))
            {
                SubstituteExtensions.DecrementRefCount(originalType);
            }
        }

        /// <summary>
        /// Clear all registered interceptors and decrement all reference counts
        /// </summary>
        internal static void ClearAllInterceptors()
        {
            foreach (var interceptor in _interceptors.Values)
            {
                interceptor.Cleanup();
            }

            foreach (var kvp in _substituteTypes)
            {
                SubstituteExtensions.DecrementRefCount(kvp.Value);
            }

            _interceptors.Clear();
            _substituteTypes.Clear();
        }

        /// <summary>
        /// Get the count of registered interceptors
        /// </summary>
        internal static int GetInterceptorCount()
        {
            return _interceptors.Count;
        }
    }
}