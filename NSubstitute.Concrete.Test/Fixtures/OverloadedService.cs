namespace NSubstitute.Concrete.Test.Fixtures;

/// <summary>Two methods with the same name, and two methods with different names but one argument shape.</summary>
public class OverloadedService
{
    public string Describe(int value) => $"int:{value}";

    public string Describe(string value) => $"string:{value}";

    public string A(int value) => $"A:{value}";

    public string B(int value) => $"B:{value}";

    protected virtual string Protected(int value) => $"protected:{value}";

    public string CallProtected(int value) => Protected(value);
}

/// <summary>A type whose equality is by value, so two instances of it compare equal.</summary>
public class ValueEqualService
{
    public ValueEqualService(string key) => Key = key;

    public string Key { get; }

    public string Read() => "real:" + Key;

    public override bool Equals(object? obj) => obj is ValueEqualService other && other.Key == Key;

    public override int GetHashCode() => Key.GetHashCode();
}

public static class OverloadedStatics
{
    public static string Describe(int value) => $"int:{value}";

    public static string Describe(string value) => $"string:{value}";
}

public static class ScopedStaticsOne
{
    public static string Read() => "real one";
}

public static class ScopedStaticsTwo
{
    public static string Read() => "real two";
}
