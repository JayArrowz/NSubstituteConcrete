using System;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Interface for async method setup configuration with full callback support
/// </summary>
public interface IAsyncMethodSetup<T, TResult> where T : class
{
    // Basic return values
    T Returns(TResult value);
    T Returns(Task<TResult> task);
    T Returns(Func<TResult> valueFactory);
    T Returns<T1>(Func<T1, TResult> valueFactory);

    // Async return values
    T Returns(Func<Task<TResult>> asyncFactory);
    T Returns<T1>(Func<T1, Task<TResult>> asyncFactory);

    // Basic callbacks
    T Callback(Action callback);
    T Callback<T1>(Action<T1> callback);

    // Async callbacks
    T CallbackAsync(Func<Task> asyncCallback);
    T CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Combined callbacks and return values
    T ReturnsAndCallback(TResult value, Action callback);
    T ReturnsAndCallbackAsync(TResult value, Func<Task> asyncCallback);

    // Exception throwing
    T Throws<TException>() where TException : Exception, new();
    T Throws<TException>(TException exception) where TException : Exception;

    // Sequence support
    T ReturnsInOrder(params TResult[] values);
}