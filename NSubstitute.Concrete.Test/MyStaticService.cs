namespace NSubstitute.Concrete.Test;

public static class MyStaticService
{
    public static string GetStaticName() => "Real Static Name";
    public static Task<string> GetStaticNameAsync() => Task.FromResult("Real Static Async Name");
    public static void DoStaticSomething() { /* Real static implementation */ }
    public static async Task DoStaticSomethingAsync() { /* Real static async implementation */ }
    public static int Calculate(int a, int b) => a + b;
}
