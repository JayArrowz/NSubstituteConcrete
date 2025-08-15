using NSubstitute.Concrete.Callbacks;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Statics;

/// <summary>
/// Implementation of static method setup with return values
/// </summary>
public class StaticMethodSetup<TResult> : IStaticMethodSetup<TResult>
{
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public StaticMethodSetup(MethodInfo method, object[] arguments)
    {
        _method = method;
        _arguments = arguments;
    }

    public void Returns(TResult value)
    {
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, value);
    }

    public void Returns(params TResult[] values)
    {
        if (values?.Length > 0)
        {
            var sequence = new ValueSequence<TResult>(values);
            StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, sequence);
        }
    }

    public void Returns(Func<TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapper<TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1>(Func<T1, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapper<T1, TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1, T2>(Func<T1, T2, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapperT1T2<T1, T2, TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1, T2, T3>(Func<T1, T2, T3, TResult> valueFactory)
    {
        var wrapper = new FunctionCallbackWrapperT1T2T3<T1, T2, T3, TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns(Func<Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(asyncFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1>(Func<T1, Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(asyncFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback(Action callback)
    {
        var wrapper = new CallbackWrapper(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1>(Action<T1> callback)
    {
        var wrapper = new CallbackWrapper<T1>(callback, typeof(TResult));
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1, T2>(Action<T1, T2> callback)
    {
        var wrapper = new CallbackWrapperT1T2<T1, T2>(callback, typeof(TResult));
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1, T2, T3>(Action<T1, T2, T3> callback)
    {
        var wrapper = new CallbackWrapperT1T2T3<T1, T2, T3>(callback, typeof(TResult));
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync(Func<Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapper(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void ReturnsAndCallback(TResult value, Action callback)
    {
        var wrapper = new CallbackAndReturnWrapper<TResult>(value, callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void ReturnsAndCallback<T1>(Func<T1, TResult> valueFactory, Action<T1> callback)
    {
        var wrapper = new CallbackAndReturnWrapperT1<T1, TResult>(valueFactory, callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Throws<TException>() where TException : Exception, new()
    {
        var wrapper = new ExceptionWrapper<TException>();
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Throws<TException>(TException exception) where TException : Exception
    {
        var wrapper = new ExceptionWrapper<TException>(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void ReturnsInOrder(params TResult[] values)
    {
        Returns(values);
    }
}

/// <summary>
/// Implementation of static void method setup
/// </summary>
public class StaticVoidMethodSetup : IStaticVoidMethodSetup
{
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public StaticVoidMethodSetup(MethodInfo method, object[] arguments)
    {
        _method = method;
        _arguments = arguments;
    }

    public void Callback(Action callback)
    {
        var wrapper = new CallbackWrapper(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1>(Action<T1> callback)
    {
        var wrapper = new CallbackWrapper<T1>(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1, T2>(Action<T1, T2> callback)
    {
        var wrapper = new CallbackWrapperT1T2<T1, T2>(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback<T1, T2, T3>(Action<T1, T2, T3> callback)
    {
        var wrapper = new CallbackWrapperT1T2T3<T1, T2, T3>(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync(Func<Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapper(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Throws<TException>() where TException : Exception, new()
    {
        var wrapper = new ExceptionWrapper<TException>();
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Throws<TException>(TException exception) where TException : Exception
    {
        var wrapper = new ExceptionWrapper<TException>(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }
}

/// <summary>
/// Implementation of static async method setup
/// </summary>
public class StaticAsyncMethodSetup<TResult> : IStaticAsyncMethodSetup<TResult>
{
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public StaticAsyncMethodSetup(MethodInfo method, object[] arguments)
    {
        _method = method;
        _arguments = arguments;
    }

    public void Returns(TResult value)
    {
        var task = Task.FromResult(value);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Returns(Task<TResult> task)
    {
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Returns(Func<TResult> valueFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1>(Func<T1, TResult> valueFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(valueFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns(Func<Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(asyncFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Returns<T1>(Func<T1, Task<TResult>> asyncFactory)
    {
        var wrapper = new AsyncFunctionCallbackWrapperT1<T1, TResult>(asyncFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Callback(Action callback)
    {
        var task = Task.Run(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Callback<T1>(Action<T1> callback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(arg =>
        {
            callback(arg);
            return Task.CompletedTask;
        });
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync(Func<Task> asyncCallback)
    {
        var wrapper = new AsyncFunctionCallbackWrapper<TResult>(async () =>
        {
            await asyncCallback();
            return default;
        });
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void ReturnsAndCallback(TResult value, Action callback)
    {
        var task = Task.Run(() =>
        {
            callback();
            return value;
        });
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void ReturnsAndCallbackAsync(TResult value, Func<Task> asyncCallback)
    {
        var task = asyncCallback().ContinueWith(_ => value);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Throws<TException>() where TException : Exception, new()
    {
        var exception = new TException();
        var faultedTask = Task.FromException<TResult>(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, faultedTask);
    }

    public void Throws<TException>(TException exception) where TException : Exception
    {
        var faultedTask = Task.FromException<TResult>(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, faultedTask);
    }

    public void ReturnsInOrder(params TResult[] values)
    {
        var taskValues = new Task<TResult>[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            taskValues[i] = Task.FromResult(values[i]);
        }

        var sequence = new ValueSequence<Task<TResult>>(taskValues);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, sequence);
    }
}

/// <summary>
/// Implementation of static void async method setup
/// </summary>
public class StaticVoidAsyncMethodSetup : IStaticVoidAsyncMethodSetup
{
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public StaticVoidAsyncMethodSetup(MethodInfo method, object[] arguments)
    {
        _method = method;
        _arguments = arguments;
    }

    public void Returns()
    {
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, Task.CompletedTask);
    }

    public void Returns(Task task)
    {
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Callback(Action callback)
    {
        var task = Task.Run(callback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, task);
    }

    public void Callback<T1>(Action<T1> callback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(arg =>
        {
            callback(arg);
            return Task.CompletedTask;
        });
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync(Func<Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapper(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void CallbackAsync<T1>(Func<T1, Task> asyncCallback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(asyncCallback);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void Throws<TException>() where TException : Exception, new()
    {
        var exception = new TException();
        var faultedTask = Task.FromException(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, faultedTask);
    }

    public void Throws<TException>(TException exception) where TException : Exception
    {
        var faultedTask = Task.FromException(exception);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, faultedTask);
    }

    public void DelayAndCallback(TimeSpan delay, Action callback)
    {
        Func<Task> taskFactory = () => Task.Delay(delay).ContinueWith(_ => callback());
        var wrapper = new AsyncCallbackWrapper(taskFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }

    public void DelayAndCallbackAsync(TimeSpan delay, Func<Task> asyncCallback)
    {
        Func<Task> taskFactory = async () =>
        {
            await Task.Delay(delay);
            await asyncCallback();
        };
        var wrapper = new AsyncCallbackWrapper(taskFactory);
        StaticMethodInterceptor.Instance.ConfigureReturn(_method, _arguments, wrapper);
    }
}