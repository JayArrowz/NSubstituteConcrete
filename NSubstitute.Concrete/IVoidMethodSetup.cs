using System.Threading.Tasks;
using System;

namespace NSubstitute.Concrete;

public interface IVoidMethodSetup<T> where T : class
{
    // Basic callbacks
    T Callback(Action callback);
    T Callback<T1>(Action<T1> callback);
    T Callback<T1, T2>(Action<T1, T2> callback);
    T Callback<T1, T2, T3>(Action<T1, T2, T3> callback);

    // Async callbacks
    T CallbackAsync(Func<Task> asyncCallback);
    T CallbackAsync<T1>(Func<T1, Task> asyncCallback);

    // Exception throwing
    T Throws<TException>() where TException : Exception, new();
    T Throws<TException>(TException exception) where TException : Exception;
}