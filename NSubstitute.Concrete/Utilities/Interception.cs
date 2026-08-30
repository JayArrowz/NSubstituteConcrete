using System;
using System.Reflection;
using System.Threading;

namespace NSubstitute.Concrete.Utilities;

/// <summary>
/// Answers an intercepted call when nothing in the interceptor's own configuration matches.
/// <para>
/// Return <see cref="Interception.RunOriginal"/> to let the real method run; return anything else
/// (including <c>null</c>) to use it as the call's result. This is the single seam a host library
/// needs in order to layer strict behaviour, default-value providers or its own setup engine on top
/// of the patching, without this library knowing about any of them.
/// </para>
/// </summary>
/// <param name="method">The method that was called.</param>
/// <param name="instance">The instance the call was made on, or <c>null</c> for a static method.</param>
/// <param name="arguments">The call's arguments.</param>
public delegate object InterceptionFallback(MethodBase method, object instance, object[] arguments);

/// <summary>Shared interception plumbing: the "run the original" signal and the call ordinal.</summary>
public static class Interception
{
    private static long _ordinal;

    /// <summary>
    /// The sentinel an <see cref="InterceptionFallback"/> returns to say it did not answer the call,
    /// so the real method should run. Distinct from <c>null</c>, which is a legitimate result.
    /// </summary>
    public static readonly object RunOriginal = new object();

    /// <summary>The next call ordinal, monotonic across every interceptor in the process.</summary>
    public static long NextOrdinal() => Interlocked.Increment(ref _ordinal);
}
