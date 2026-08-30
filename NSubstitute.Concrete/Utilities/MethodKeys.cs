using System.Linq;
using System.Reflection;

namespace NSubstitute.Concrete.Utilities;

/// <summary>
/// Builds the key a method's configuration and recorded calls are filed under.
/// <para>
/// The key is the full signature, not the method name: two overloads of the same name are different
/// methods and must not share a configuration bucket.
/// </para>
/// </summary>
public static class MethodKeys
{
    /// <summary>The signature key for <paramref name="method"/>, or the empty string when it is null.</summary>
    public static string For(MethodBase method)
    {
        if (method == null) return string.Empty;

        var parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName ?? p.ParameterType.Name));

        var genericArgs = string.Empty;
        if (method.IsGenericMethod)
        {
            var typeArgs = method.GetGenericArguments();
            genericArgs = $"<{string.Join(",", typeArgs.Select(t => t.FullName ?? t.Name))}>";
        }

        return $"{method.DeclaringType?.FullName}.{method.Name}{genericArgs}({parameters})";
    }

    /// <summary>
    /// The key used when a method is configured by name alone, which cannot distinguish overloads.
    /// Kept separate from <see cref="For(MethodBase)"/> so a name-only configuration never shadows a
    /// signature-accurate one.
    /// </summary>
    public static string ForName(string methodName) => "name:" + methodName;
}
