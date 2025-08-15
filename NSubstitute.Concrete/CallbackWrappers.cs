using System;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Base interface for all wrapper types
/// </summary>
public interface ICallbackWrapper
{
    object Execute(object[] arguments);
}

/// <summary>
/// Wrapper for void callbacks with no parameters
/// </summary>
public class CallbackWrapper : ICallbackWrapper
{
    private readonly Action _callback;

    public CallbackWrapper(Action callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        _callback();
        return null;
    }
}

/// <summary>
/// Wrapper for void callbacks with one parameter
/// </summary>
public class CallbackWrapper<T1> : ICallbackWrapper
{
    private readonly Action<T1> _callback;

    public CallbackWrapper(Action<T1> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        _callback(arg1);
        return null;
    }
}

/// <summary>
/// Wrapper for void callbacks with two parameters
/// </summary>
public class CallbackWrapperT1T2<T1, T2> : ICallbackWrapper
{
    private readonly Action<T1, T2> _callback;

    public CallbackWrapperT1T2(Action<T1, T2> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        var arg2 = arguments.Length > 1 ? (T2)arguments[1] : default(T2);
        _callback(arg1, arg2);
        return null;
    }
}

/// <summary>
/// Wrapper for void callbacks with three parameters
/// </summary>
public class CallbackWrapperT1T2T3<T1, T2, T3> : ICallbackWrapper
{
    private readonly Action<T1, T2, T3> _callback;

    public CallbackWrapperT1T2T3(Action<T1, T2, T3> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        var arg2 = arguments.Length > 1 ? (T2)arguments[1] : default(T2);
        var arg3 = arguments.Length > 2 ? (T3)arguments[2] : default(T3);
        _callback(arg1, arg2, arg3);
        return null;
    }
}

/// <summary>
/// Wrapper for function callbacks with no parameters
/// </summary>
public class FunctionCallbackWrapper<TResult> : ICallbackWrapper
{
    private readonly Func<TResult> _callback;

    public FunctionCallbackWrapper(Func<TResult> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        return _callback();
    }
}

/// <summary>
/// Wrapper for function callbacks with one parameter
/// </summary>
public class FunctionCallbackWrapper<T1, TResult> : ICallbackWrapper
{
    private readonly Func<T1, TResult> _callback;

    public FunctionCallbackWrapper(Func<T1, TResult> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        return _callback(arg1);
    }
}

/// <summary>
/// Wrapper for function callbacks with two parameters
/// </summary>
public class FunctionCallbackWrapperT1T2<T1, T2, TResult> : ICallbackWrapper
{
    private readonly Func<T1, T2, TResult> _callback;

    public FunctionCallbackWrapperT1T2(Func<T1, T2, TResult> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        var arg2 = arguments.Length > 1 ? (T2)arguments[1] : default(T2);
        return _callback(arg1, arg2);
    }
}

/// <summary>
/// Wrapper for function callbacks with three parameters
/// </summary>
public class FunctionCallbackWrapperT1T2T3<T1, T2, T3, TResult> : ICallbackWrapper
{
    private readonly Func<T1, T2, T3, TResult> _callback;

    public FunctionCallbackWrapperT1T2T3(Func<T1, T2, T3, TResult> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        var arg2 = arguments.Length > 1 ? (T2)arguments[1] : default(T2);
        var arg3 = arguments.Length > 2 ? (T3)arguments[2] : default(T3);
        return _callback(arg1, arg2, arg3);
    }
}

/// <summary>
/// Wrapper for function callbacks with four parameters
/// </summary>
public class FunctionCallbackWrapperT1T2T3T4<T1, T2, T3, T4, TResult> : ICallbackWrapper
{
    private readonly Func<T1, T2, T3, T4, TResult> _callback;

    public FunctionCallbackWrapperT1T2T3T4(Func<T1, T2, T3, T4, TResult> callback)
    {
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        var arg2 = arguments.Length > 1 ? (T2)arguments[1] : default(T2);
        var arg3 = arguments.Length > 2 ? (T3)arguments[2] : default(T3);
        var arg4 = arguments.Length > 3 ? (T4)arguments[3] : default(T4);
        return _callback(arg1, arg2, arg3, arg4);
    }
}

/// <summary>
/// Wrapper for combined callback and return value
/// </summary>
public class CallbackAndReturnWrapper<TResult> : ICallbackWrapper
{
    private readonly TResult _value;
    private readonly Action _callback;

    public CallbackAndReturnWrapper(TResult value, Action callback)
    {
        _value = value;
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        _callback();
        return _value;
    }
}

/// <summary>
/// Wrapper for combined callback and return value with parameters
/// </summary>
public class CallbackAndReturnWrapperT1<T1, TResult> : ICallbackWrapper
{
    private readonly Func<T1, TResult> _valueFactory;
    private readonly Action<T1> _callback;

    public CallbackAndReturnWrapperT1(Func<T1, TResult> valueFactory, Action<T1> callback)
    {
        _valueFactory = valueFactory;
        _callback = callback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        _callback(arg1);
        return _valueFactory(arg1);
    }
}

/// <summary>
/// Wrapper for async callbacks with no parameters
/// </summary>
public class AsyncCallbackWrapper : ICallbackWrapper
{
    private readonly Func<Task> _asyncCallback;

    public AsyncCallbackWrapper(Func<Task> asyncCallback)
    {
        _asyncCallback = asyncCallback;
    }

    public object Execute(object[] arguments)
    {
        return _asyncCallback();
    }
}

/// <summary>
/// Wrapper for async callbacks with one parameter
/// </summary>
public class AsyncCallbackWrapperT1<T1> : ICallbackWrapper
{
    private readonly Func<T1, Task> _asyncCallback;

    public AsyncCallbackWrapperT1(Func<T1, Task> asyncCallback)
    {
        _asyncCallback = asyncCallback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);
        return _asyncCallback(arg1);
    }
}

/// <summary>
/// Wrapper for async function callbacks with no parameters
/// </summary>
public class AsyncFunctionCallbackWrapper<TResult> : ICallbackWrapper
{
    private readonly Func<TResult> _syncCallback;
    private readonly Func<Task<TResult>> _asyncCallback;

    public AsyncFunctionCallbackWrapper(Func<TResult> syncCallback)
    {
        _syncCallback = syncCallback;
    }

    public AsyncFunctionCallbackWrapper(Func<Task<TResult>> asyncCallback)
    {
        _asyncCallback = asyncCallback;
    }

    public object Execute(object[] arguments)
    {
        if (_asyncCallback != null)
        {
            return _asyncCallback();
        }
        else
        {
            var result = _syncCallback();
            return Task.FromResult(result);
        }
    }
}

/// <summary>
/// Wrapper for async function callbacks with one parameter
/// </summary>
public class AsyncFunctionCallbackWrapperT1<T1, TResult> : ICallbackWrapper
{
    private readonly Func<T1, TResult> _syncCallback;
    private readonly Func<T1, Task<TResult>> _asyncCallback;

    public AsyncFunctionCallbackWrapperT1(Func<T1, TResult> syncCallback)
    {
        _syncCallback = syncCallback;
    }

    public AsyncFunctionCallbackWrapperT1(Func<T1, Task<TResult>> asyncCallback)
    {
        _asyncCallback = asyncCallback;
    }

    public object Execute(object[] arguments)
    {
        var arg1 = arguments.Length > 0 ? (T1)arguments[0] : default(T1);

        if (_asyncCallback != null)
        {
            return _asyncCallback(arg1);
        }
        else
        {
            var result = _syncCallback(arg1);
            return Task.FromResult(result);
        }
    }
}

/// <summary>
/// Wrapper for exception throwing
/// </summary>
public class ExceptionWrapper<TException> : ICallbackWrapper where TException : Exception
{
    private readonly TException _exception;

    public ExceptionWrapper()
    {
        _exception = Activator.CreateInstance<TException>();
    }

    public ExceptionWrapper(TException exception)
    {
        _exception = exception;
    }

    public object Execute(object[] arguments)
    {
        throw _exception;
    }
}

/// <summary>
/// Wrapper for value sequences (ReturnsInOrder)
/// </summary>
public class ValueSequence<TResult> : ICallbackWrapper
{
    private readonly TResult[] _values;
    private int _currentIndex = 0;
    private readonly object _lock = new object();

    public ValueSequence(params TResult[] values)
    {
        _values = values ?? throw new ArgumentNullException(nameof(values));
        if (_values.Length == 0)
            throw new ArgumentException("Value sequence cannot be empty", nameof(values));
    }

    public object Execute(object[] arguments)
    {
        lock (_lock)
        {
            if (_currentIndex >= _values.Length)
            {
                // Return the last value for any subsequent calls
                return _values[_values.Length - 1];
            }

            var value = _values[_currentIndex];
            _currentIndex++;
            return value;
        }
    }
}