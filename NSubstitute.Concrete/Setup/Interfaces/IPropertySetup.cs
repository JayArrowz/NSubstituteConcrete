namespace NSubstitute.Concrete.Setup.Interfaces;

/// <summary>
/// Interface for property setup configuration
/// </summary>
public interface IPropertySetup<T, TResult> where T : class
{
    T Returns(TResult value);
}