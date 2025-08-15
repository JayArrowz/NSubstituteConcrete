using NSubstitute.Concrete.Callbacks;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Setup.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Setup.Instance;

/// <summary>
/// Implementation of method setup with full async support and callbacks
/// </summary>
public class MethodSetup<T, TResult> : IMethodSetup<T, TResult> where T : class
{
    private readonly ConcreteMethodInterceptor _interceptor;
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public MethodSetup(ConcreteMethodInterceptor interceptor, MethodInfo method, object[] arguments)
    {
        _interceptor = interceptor;
        _method = method;
        _arguments = arguments;
    }

    public T Returns(TResult value)
    {
        if (IsAsyncMethod(_method))
        {
            if (value is Task)
            {
                _interceptor.ConfigureReturn(_method, _arguments, value);
            }
            else
            {
                var wrappedValue = WrapInTaskIfNeeded(value, _method.ReturnType);
                _interceptor.ConfigureReturn(_method, _arguments, wrappedValue);
            }
        }
        else
        {
            _interceptor.ConfigureReturn(_method, _arguments, value);
        }

        return default;
    }

    public T Returns(params TResult[] values)
    {
        if (values?.Length > 0)
        {
            var sequence = new ValueSequence<TResult>(values);
            _interceptor.ConfigureReturn(_method, _arguments, sequence);
        }
        return default;
    }

    public T Returns(Func<TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapper<TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Returns<T1>(Func<T1, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapper<T1, TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Returns<T1, T2>(Func<T1, T2, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapperT1T2<T1, T2, TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Returns<T1, T2, T3>(Func<T1, T2, T3, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapperT1T2T3<T1, T2, T3, TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Returns<T1, T2, T3, T4>(Func<T1, T2, T3, T4, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapperT1T2T3T4<T1, T2, T3, T4, TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    // New callback methods
    public T Callback(Action callback)
    {
        var wrapper = new CallbackWrapper(callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Callback<T1>(Action<T1> callback)
    {
        var wrapper = new CallbackWrapper<T1>(callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Callback<T1, T2>(Action<T1, T2> callback)
    {
        var wrapper = new CallbackWrapperT1T2<T1, T2>(callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Callback<T1, T2, T3>(Action<T1, T2, T3> callback)
    {
        var wrapper = new CallbackWrapperT1T2T3<T1, T2, T3>(callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T ReturnsAndCallback(TResult value, Action callback)
    {
        var wrapper = new CallbackAndReturnWrapper<TResult>(value, callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T ReturnsAndCallback<T1>(Func<T1, TResult> valueFactory, Action<T1> callback)
    {
        var wrapper = new CallbackAndReturnWrapperT1<T1, TResult>(valueFactory, callback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Throws<TException>() where TException : Exception, new()
    {
        var wrapper = new ExceptionWrapper<TException>();
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T Throws<TException>(TException exception) where TException : Exception
    {
        var wrapper = new ExceptionWrapper<TException>(exception);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T ReturnsInOrder(params TResult[] values)
    {
        return Returns(values);
    }

    public T Returns(Func<Task<TResult>> asyncFactory)
    {
        if (IsAsyncMethod(_method))
        {
            var wrapper = new AsyncFunctionCallbackWrapper<TResult>(asyncFactory);
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        else
        {
            var wrapper = new FunctionCallbackWrapper<TResult>(() => asyncFactory().GetAwaiter().GetResult());
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        return default;
    }

    public T Returns<T1>(Func<T1, Task<TResult>> asyncFactory)
    {
        if (IsAsyncMethod(_method))
        {
            var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(asyncFactory);
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        else
        {
            var wrapper = new FunctionCallbackWrapper<T1, TResult>(arg => asyncFactory(arg).GetAwaiter().GetResult());
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        return default;
    }

    public T CallbackAsync(Func<Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapper(asyncCallback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    private bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task) ||
               method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);
    }

    private object WrapInTaskIfNeeded(TResult value, Type returnType)
    {
        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var taskResultType = returnType.GetGenericArguments()[0];
            if (value == null && !taskResultType.IsValueType)
            {
                var fromResultMethod = typeof(Task).GetMethod("FromResult").MakeGenericMethod(taskResultType);
                return fromResultMethod.Invoke(null, new object[] { null });
            }

            object convertedValue = value;
            if (value != null && !taskResultType.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    convertedValue = Convert.ChangeType(value, taskResultType);
                }
                catch
                {
                    convertedValue = value;
                }
            }

            var fromResultMethod2 = typeof(Task).GetMethod("FromResult").MakeGenericMethod(taskResultType);
            return fromResultMethod2.Invoke(null, new object[] { convertedValue });
        }

        return value;
    }
}