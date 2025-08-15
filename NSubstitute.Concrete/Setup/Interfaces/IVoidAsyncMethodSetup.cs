using System;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Setup.Interfaces;

/// <summary>
/// Interface for void async method setup configuration with full callback support
/// </summary>
public interface IVoidAsyncMethodSetup<T> where T : class
{
    // Basic returns
    T Returns();
    T Returns(Task task);

    // Basic callbacks
    T Callback(Action callback);
    T Callback<T1>(Action<T1> callback);

    // Async callbacks
    T CallbackAsync(Func<Task> asyncCallback);
    T CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Exception throwing
    T Throws<TException>() where TException : Exception, new();
    T Throws<TException>(TException exception) where TException : Exception;

    // Delayed callbacks
    T DelayAndCallback(TimeSpan delay, Action callback);
    T DelayAndCallbackAsync(TimeSpan delay, Func<Task> asyncCallback);
}