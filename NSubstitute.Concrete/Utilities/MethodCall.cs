using System.Reflection;

using System;

namespace NSubstitute.Concrete.Utilities;

/// <summary>
/// Represents a method call for verification
/// </summary>
public class MethodCall
{
    public MethodInfo Method { get; set; }
    public object[] Arguments { get; set; }
    public object Target { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Position of this call in the order every interceptor in the process observed.
    /// <see cref="CalledAt"/> has no useful resolution for ordering — consecutive calls routinely
    /// share a timestamp — so ordering is done on this instead.
    /// </summary>
    public long Ordinal { get; set; }
}
