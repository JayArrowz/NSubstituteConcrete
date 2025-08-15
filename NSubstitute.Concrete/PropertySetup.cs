using System.Reflection;

namespace NSubstitute.Concrete;

/// <summary>
/// Implementation of property setup
/// </summary>
public class PropertySetup<T, TResult> : IPropertySetup<T, TResult> where T : class
{
    private readonly ConcreteMethodInterceptor _interceptor;
    private readonly PropertyInfo _property;

    public PropertySetup(ConcreteMethodInterceptor interceptor, PropertyInfo property)
    {
        _interceptor = interceptor;
        _property = property;
    }

    public T Returns(TResult value)
    {
        _interceptor.ConfigureProperty(_property.Name, value);
        return default(T);
    }
}