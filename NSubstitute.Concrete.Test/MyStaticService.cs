namespace NSubstitute.Concrete.Test;

public static class MyStaticService
{
    public static string GetStaticName() => "Real Static Name";
    public static int Calculate(int a, int b) => a + b;
    public static void DoSomething() { /* Real implementation */ }
    public static async Task<string> GetNameAsync() => await Task.FromResult("Real Async Name");
    public static async Task DoSomethingAsync() => await Task.Delay(1);
    public static bool IsValid(string input) => !string.IsNullOrEmpty(input);
    public static DateTime GetCurrentTime() => DateTime.Now;
    public static T ProcessGeneric<T>(T input) => input;
}