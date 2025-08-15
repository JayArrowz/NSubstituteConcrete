using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Implementation of async method setup with automatic Task wrapping and callbacks
/// </summary>
public class AsyncMethodSetup<T, TResult> : IAsyncMethodSetup<T, TResult> where T : class
{
    private readonly ConcreteMethodInterceptor _interceptor;
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public AsyncMethodSetup(ConcreteMethodInterceptor interceptor, MethodInfo method, object[] arguments)
    {
        _interceptor = interceptor;
        _method = method;
        _arguments = arguments;
    }

    public T Returns(TResult value)
    {
        var task = Task.FromResult(value);
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default(T);
    }

    public T Returns(Task<TResult> task)
    {
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default(T);
    }

    public T Returns(Func<TResult> valueFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    public T Returns<T1>(Func<T1, TResult> valueFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(valueFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    public T Returns(Func<Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(asyncFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    public T Returns<T1>(Func<T1, Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(asyncFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    public T Callback(Action callback)
    {
        var task = Task.Run(callback);
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default(T);
    }

    public T Callback<T1>(Action<T1> callback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(arg =>
        {
            callback(arg);
            return Task.CompletedTask;
        });
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    private bool IsAsyncMethod(MethodInfo method)
    {
        return method.ReturnType == typeof(Task) ||
               (method.ReturnType.IsGenericType &&
                method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
    }

    public T CallbackAsync(Func<Task> asyncCallback)
    {
        if (IsAsyncMethod(_method))
        {
            var wrapper = new AsyncFunctionCallbackWrapper<TResult>(async () =>
            {
                await asyncCallback();
                return default;
            });
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        else
        {
            var wrapper = new AsyncCallbackWrapper(asyncCallback);
            _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        }
        return default(T);
    }

    public T CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default(T);
    }

    public T ReturnsAndCallback(TResult value, Action callback)
    {
        var task = Task.Run(() =>
        {
            callback();
            return value;
        });
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default(T);
    }

    public T ReturnsAndCallbackAsync(TResult value, Func<Task> asyncCallback)
    {
        var task = asyncCallback().ContinueWith(_ => value);
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default(T);
    }

    public T Throws<TException>() where TException : Exception, new()
    {
        var exception = new TException();
        var faultedTask = Task.FromException<TResult>(exception);
        _interceptor.ConfigureReturn(_method, _arguments, faultedTask);
        return default(T);
    }

    public T Throws<TException>(TException exception) where TException : Exception
    {
        var faultedTask = Task.FromException<TResult>(exception);
        _interceptor.ConfigureReturn(_method, _arguments, faultedTask);
        return default(T);
    }

    public T ReturnsInOrder(params TResult[] values)
    {
        var taskValues = new Task<TResult>[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            taskValues[i] = Task.FromResult(values[i]);
        }

        var sequence = new ValueSequence<Task<TResult>>(taskValues);
        _interceptor.ConfigureReturn(_method, _arguments, sequence);
        return default(T);
    }
}