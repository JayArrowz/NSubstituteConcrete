using System.Reflection;

using System;

namespace NSubstitute.Concrete;

/// <summary>
/// Represents a method call for verification
/// </summary>
public class MethodCall
{
    public MethodInfo Method { get; set; }
    public object[] Arguments { get; set; }
    public object Target { get; set; }
    public DateTime CalledAt { get; set; } = DateTime.UtcNow;
}