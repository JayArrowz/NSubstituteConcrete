namespace NSubstitute.Concrete.Utilities;

/// <summary>
/// Helper class for argument matching
/// </summary>
public static class It
{
    public static T IsAny<T>() => default;
    public static T Is<T>(T value) => value;
}