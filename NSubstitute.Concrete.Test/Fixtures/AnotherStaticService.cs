namespace NSubstitute.Concrete.Test.Fixtures;

public static class AnotherStaticService
{
    public static string GetValue() => "Another Value";
    public static int Multiply(int x, int y) => x * y;
}
