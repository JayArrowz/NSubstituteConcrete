namespace NSubstitute.Concrete;

/// <summary>
/// Helper class for argument matching
/// </summary>
public static class It
{
    public static T IsAny<T>() => default(T);
    public static T Is<T>(T value) => value;
}