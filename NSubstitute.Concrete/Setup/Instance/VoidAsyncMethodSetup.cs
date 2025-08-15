using NSubstitute.Concrete.Callbacks;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Setup.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Setup.Instance;

/// <summary>
/// Implementation of void async method setup with callback support
/// </summary>
public class VoidAsyncMethodSetup<T> : IVoidAsyncMethodSetup<T> where T : class
{
    private readonly ConcreteMethodInterceptor _interceptor;
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public VoidAsyncMethodSetup(ConcreteMethodInterceptor interceptor, MethodInfo method, object[] arguments)
    {
        _interceptor = interceptor;
        _method = method;
        _arguments = arguments;
    }

    public T Returns()
    {
        _interceptor.ConfigureReturn(_method, _arguments, Task.CompletedTask);
        return default;
    }

    public T Returns(Task task)
    {
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default;
    }

    public T Callback(Action callback)
    {
        var task = Task.Run(callback);
        _interceptor.ConfigureReturn(_method, _arguments, task);
        return default;
    }

    public T Callback<T1>(Action<T1> callback)
    {
        var wrapper = new AsyncCallbackWrapperT1<T1>(arg =>
        {
            callback(arg);
            return Task.CompletedTask;
        });
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
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

    public T Throws<TException>() where TException : Exception, new()
    {
        var exception = new TException();
        var faultedTask = Task.FromException(exception);
        _interceptor.ConfigureReturn(_method, _arguments, faultedTask);
        return default;
    }

    public T Throws<TException>(TException exception) where TException : Exception
    {
        var faultedTask = Task.FromException(exception);
        _interceptor.ConfigureReturn(_method, _arguments, faultedTask);
        return default;
    }

    public T DelayAndCallback(TimeSpan delay, Action callback)
    {
        Func<Task> taskFactory = () => Task.Delay(delay).ContinueWith(_ => callback());
        var wrapper = new AsyncCallbackWrapper(taskFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }

    public T DelayAndCallbackAsync(TimeSpan delay, Func<Task> asyncCallback)
    {
        Func<Task> taskFactory = async () =>
        {
            await Task.Delay(delay);
            await asyncCallback();
        };
        var wrapper = new AsyncCallbackWrapper(taskFactory);
        _interceptor.ConfigureReturn(_method, _arguments, wrapper);
        return default;
    }
}