using NSubstitute.Concrete.Callbacks;
using NSubstitute.Concrete.Core;
using NSubstitute.Concrete.Setup.Interfaces;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Setup.Instance;

public class VoidMethodSetup<T> : IVoidMethodSetup<T> where T : class
{
    private readonly ConcreteMethodInterceptor _interceptor;
    private readonly MethodInfo _method;
    private readonly object[] _arguments;

    public VoidMethodSetup(
        ConcreteMethodInterceptor interceptor,
        MethodInfo method,
        object[] arguments)
    {
        _interceptor = interceptor;
        _method = method;
        _arguments = arguments;
    }

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
}
