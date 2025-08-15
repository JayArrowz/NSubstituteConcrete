using System;
using System.Threading.Tasks;

namespace NSubstitute.Concrete.Statics;

/// <summary>
/// Interface for static method setup configuration
/// </summary>
public interface IStaticMethodSetup<TResult>
{
    // Basic returns
    void Returns(TResult value);
    void Returns(params TResult[] values);
    void Returns(Func<TResult> valueFactory);
    void Returns<T1>(Func<T1, TResult> valueFactory);
    void Returns<T1, T2>(Func<T1, T2, TResult> valueFactory);
    void Returns<T1, T2, T3>(Func<T1, T2, T3, TResult> valueFactory);

    // Async returns
    void Returns(Func<Task<TResult>> asyncFactory);
    void Returns<T1>(Func<T1, Task<TResult>> asyncFactory);

    // Basic callbacks
    void Callback(Action callback);
    void Callback<T1>(Action<T1> callback);
    void Callback<T1, T2>(Action<T1, T2> callback);
    void Callback<T1, T2, T3>(Action<T1, T2, T3> callback);

    // Async callbacks
    void CallbackAsync(Func<Task> asyncCallback);
    void CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Combined callbacks and returns
    void ReturnsAndCallback(TResult value, Action callback);
    void ReturnsAndCallback<T1>(Func<T1, TResult> valueFactory, Action<T1> callback);

    // Exception throwing
    void Throws<TException>() where TException : Exception, new();
    void Throws<TException>(TException exception) where TException : Exception;

    // Sequence returns
    void ReturnsInOrder(params TResult[] values);
}

/// <summary>
/// Interface for static void method setup configuration
/// </summary>
public interface IStaticVoidMethodSetup
{
    // Basic callbacks
    void Callback(Action callback);
    void Callback<T1>(Action<T1> callback);
    void Callback<T1, T2>(Action<T1, T2> callback);
    void Callback<T1, T2, T3>(Action<T1, T2, T3> callback);

    // Async callbacks
    void CallbackAsync(Func<Task> asyncCallback);
    void CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Exception throwing
    void Throws<TException>() where TException : Exception, new();
    void Throws<TException>(TException exception) where TException : Exception;
}

/// <summary>
/// Interface for static async method setup configuration
/// </summary>
public interface IStaticAsyncMethodSetup<TResult>
{
    // Basic returns
    void Returns(TResult value);
    void Returns(Task<TResult> task);
    void Returns(Func<TResult> valueFactory);
    void Returns<T1>(Func<T1, TResult> valueFactory);
    void Returns(Func<Task<TResult>> asyncFactory);
    void Returns<T1>(Func<T1, Task<TResult>> asyncFactory);

    // Basic callbacks
    void Callback(Action callback);
    void Callback<T1>(Action<T1> callback);

    // Async callbacks
    void CallbackAsync(Func<Task> asyncCallback);
    void CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Combined callbacks and returns
    void ReturnsAndCallback(TResult value, Action callback);
    void ReturnsAndCallbackAsync(TResult value, Func<Task> asyncCallback);

    // Exception throwing
    void Throws<TException>() where TException : Exception, new();
    void Throws<TException>(TException exception) where TException : Exception;

    // Sequence returns
    void ReturnsInOrder(params TResult[] values);
}

/// <summary>
/// Interface for static void async method setup configuration
/// </summary>
public interface IStaticVoidAsyncMethodSetup
{
    // Basic returns
    void Returns();
    void Returns(Task task);

    // Basic callbacks
    void Callback(Action callback);
    void Callback<T1>(Action<T1> callback);

    // Async callbacks
    void CallbackAsync(Func<Task> asyncCallback);
    void CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Exception throwing
    void Throws<TException>() where TException : Exception, new();
    void Throws<TException>(TException exception) where TException : Exception;

    // Delayed callbacks
    void DelayAndCallback(TimeSpan delay, Action callback);
    void DelayAndCallbackAsync(TimeSpan delay, Func<Task> asyncCallback);
}