using System;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Interface for method setup configuration with full callback support
/// </summary>
public interface IMethodSetup<T, TResult> where T : class
{
    // Basic return values
    T Returns(TResult value);
    T Returns(params TResult[] values);
    T Returns(Func<TResult> valueFactory);
    T Returns<T1>(Func<T1, TResult> valueFactory);
    T Returns<T1, T2>(Func<T1, T2, TResult> valueFactory);
    T Returns<T1, T2, T3>(Func<T1, T2, T3, TResult> valueFactory);
    T Returns<T1, T2, T3, T4>(Func<T1, T2, T3, T4, TResult> valueFactory);

    // Async return values
    T Returns(Func<Task<TResult>> asyncFactory);
    T Returns<T1>(Func<T1, Task<TResult>> asyncFactory);

    // Basic callbacks (void methods)
    T Callback(Action callback);
    T Callback<T1>(Action<T1> callback);
    T Callback<T1, T2>(Action<T1, T2> callback);
    T Callback<T1, T2, T3>(Action<T1, T2, T3> callback);

    // Async callbacks
    T CallbackAsync(Func<Task> asyncCallback);
    T CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Combined callbacks and return values
    T ReturnsAndCallback(TResult value, Action callback);
    T ReturnsAndCallback<T1>(Func<T1, TResult> valueFactory, Action<T1> callback);

    // Exception throwing
    T Throws<TException>() where TException : Exception, new();
    T Throws<TException>(TException exception) where TException : Exception;

    // Sequence support
    T ReturnsInOrder(params TResult[] values);
}