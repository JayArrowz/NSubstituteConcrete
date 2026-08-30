using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NSubstitute.Concrete.Utilities;

/// <summary>
/// Compares substitutes by identity.
/// <para>
/// The registries that map a substitute to its interceptor must not use the substituted type's own
/// <c>Equals</c>/<c>GetHashCode</c>: a class that compares by value would make two distinct
/// substitutes collide, and a mutable one would move between buckets when a setter runs.
/// </para>
/// </summary>
public sealed class ReferenceComparer : IEqualityComparer<object>
{
    /// <summary>The shared instance.</summary>
    public static readonly ReferenceComparer Instance = new ReferenceComparer();

    private ReferenceComparer() { }

    /// <inheritdoc />
    public new bool Equals(object x, object y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public int GetHashCode(object obj) => obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
}
