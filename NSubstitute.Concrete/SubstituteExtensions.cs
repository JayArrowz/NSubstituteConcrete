using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace NSubstitute.Concrete;

/// <summary>
/// Extension to NSubstitute that enables mocking of concrete classes with non-virtual methods and properties
/// </summary>
public static class SubstituteExtensions
{
    private static readonly ConcurrentDictionary<Type, Type> _proxyTypeCache = new ConcurrentDictionary<Type, Type>();
    private static readonly ConcurrentDictionary<Type, int> _proxyTypeRefCounts = new ConcurrentDictionary<Type, int>();
    private static readonly object _refCountLock = new object();

    /// <summary>
    /// Creates a substitute for a concrete class, including non-virtual methods and properties.
    /// Falls back to standard NSubstitute behavior when possible.
    /// </summary>
    public static T ForConcrete<T>(params object[] constructorArguments) where T : class
    {
        var type = typeof(T);

        // For interfaces or abstract classes, use standard NSubstitute
        if (type.IsInterface || type.IsAbstract)
        {
            return Substitute.For<T>(constructorArguments);
        }

        // Check if class has only virtual methods - use standard NSubstitute
        if (HasOnlyVirtualOrOverridableMethods(type))
        {
            return Substitute.For<T>(constructorArguments);
        }

        // For classes without parameterless constructor, ensure we have constructor arguments
        if (!HasParameterlessConstructor(type) && (constructorArguments == null || constructorArguments.Length == 0))
        {
            throw new ArgumentException($"Type {type.Name} does not have a parameterless constructor. You must provide constructor arguments.");
        }

        return CreateILProxy<T>(constructorArguments);
    }

    /// <summary>
    /// Clear the proxy type cache to free memory (useful for cleanup in long-running applications)
    /// </summary>
    public static void ClearProxyTypeCache()
    {
        lock (_refCountLock)
        {
            _proxyTypeCache.Clear();
            _proxyTypeRefCounts.Clear();
        }
    }

    /// <summary>
    /// Remove a specific type from the proxy cache (ignores reference counting)
    /// </summary>
    public static void ClearProxyType<T>() where T : class
    {
        lock (_refCountLock)
        {
            _proxyTypeCache.TryRemove(typeof(T), out _);
            _proxyTypeRefCounts.TryRemove(typeof(T), out _);
        }
    }

    /// <summary>
    /// Get the number of cached proxy types (useful for monitoring memory usage)
    /// </summary>
    public static int GetProxyTypeCacheCount()
    {
        return _proxyTypeCache.Count;
    }

    /// <summary>
    /// Get reference count for a specific type (useful for debugging)
    /// </summary>
    public static int GetRefCount<T>() where T : class
    {
        return _proxyTypeRefCounts.TryGetValue(typeof(T), out var count) ? count : 0;
    }

    /// <summary>
    /// Increment reference count for a proxy type
    /// </summary>
    private static void IncrementRefCount(Type type)
    {
        lock (_refCountLock)
        {
            _proxyTypeRefCounts.AddOrUpdate(type, 1, (key, oldValue) => oldValue + 1);
        }
    }

    /// <summary>
    /// Decrement reference count and remove proxy type if count reaches zero
    /// </summary>
    internal static void DecrementRefCount(Type type)
    {
        lock (_refCountLock)
        {
            if (_proxyTypeRefCounts.TryGetValue(type, out var currentCount))
            {
                var newCount = currentCount - 1;
                if (newCount <= 0)
                {
                    // Remove both proxy type and ref count when no more references
                    _proxyTypeCache.TryRemove(type, out _);
                    _proxyTypeRefCounts.TryRemove(type, out _);
                }
                else
                {
                    _proxyTypeRefCounts[type] = newCount;
                }
            }
        }
    }

    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool HasOnlyVirtualOrOverridableMethods(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
            .ToList();

        return methods.All(m => m.IsVirtual && !m.IsFinal);
    }

    private static T CreateILProxy<T>(object[] constructorArguments) where T : class
    {
        // Create interceptor that will handle the method calls
        var interceptor = new ConcreteMethodInterceptor();

        // Get or create the proxy type and increment reference count
        var proxyType = _proxyTypeCache.GetOrAdd(typeof(T), CreateProxyType);
        IncrementRefCount(typeof(T));

        // Create instance with interceptor
        var allArgs = constructorArguments.Concat(new object[] { interceptor }).ToArray();
        var proxy = (T)Activator.CreateInstance(proxyType, allArgs);

        // Set up the interceptor
        interceptor.SetProxy(proxy);

        // Register the interceptor so extension methods can find it
        ConcreteExtensions.RegisterInterceptor(proxy, interceptor);

        return proxy;
    }

    private static Type CreateProxyType(Type baseType)
    {
        var assemblyName = new AssemblyName($"ConcreteProxy_{baseType.Name}_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var typeBuilder = moduleBuilder.DefineType(
            $"ConcreteProxy_{baseType.Name}",
            TypeAttributes.Public | TypeAttributes.Class,
            baseType);

        var interceptorField = DefineInterceptorField(typeBuilder);
        DefineConstructor(typeBuilder, baseType, interceptorField, GetConstructorArgTypes(baseType));

        // Generate method proxies
        var methodsToProxy = GetMethodsToProxy(baseType);
        foreach (var method in methodsToProxy)
        {
            if (!method.IsSpecialName) // Skip property accessors, we'll handle them separately
            {
                DefineMethodProxy(typeBuilder, method, interceptorField);
            }
        }

        // Generate property proxies
        var propertiesToProxy = GetPropertiesToProxy(baseType);
        foreach (var property in propertiesToProxy)
        {
            DefinePropertyProxy(typeBuilder, property, interceptorField);
        }

#if NETSTANDARD2_0
        var proxyType = typeBuilder.CreateTypeInfo().AsType();
#else
        var proxyType = typeBuilder.CreateType();
#endif
        return proxyType;
    }

    private static IEnumerable<PropertyInfo> GetPropertiesToProxy(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.DeclaringType != typeof(object) && p.CanRead);
    }

    private static void DefinePropertyProxy(TypeBuilder typeBuilder, PropertyInfo property, FieldBuilder interceptorField)
    {
        // Create the property on the proxy type
        var propertyBuilder = typeBuilder.DefineProperty(
            property.Name,
            PropertyAttributes.None,
            property.PropertyType,
            null);

        // Create getter if it exists
        var getter = property.GetGetMethod();
        if (getter != null)
        {
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                                 MethodAttributes.Virtual | MethodAttributes.NewSlot;

            var getterBuilder = typeBuilder.DefineMethod(
                getter.Name,
                methodAttributes,
                property.PropertyType,
                Type.EmptyTypes);

            var getterIL = getterBuilder.GetILGenerator();

            // Load interceptor and call InterceptCall
            getterIL.Emit(OpCodes.Ldarg_0);
            getterIL.Emit(OpCodes.Ldfld, interceptorField);
            getterIL.Emit(OpCodes.Ldstr, getter.Name);
            getterIL.Emit(OpCodes.Ldc_I4_0);
            getterIL.Emit(OpCodes.Newarr, typeof(object));

            var interceptCallMethod = typeof(ConcreteMethodInterceptor).GetMethod("InterceptCall");
            getterIL.Emit(OpCodes.Callvirt, interceptCallMethod);

            // Handle return value
            if (property.PropertyType.IsValueType)
            {
                getterIL.Emit(OpCodes.Unbox_Any, property.PropertyType);
            }
            else if (property.PropertyType != typeof(object))
            {
                getterIL.Emit(OpCodes.Castclass, property.PropertyType);
            }

            getterIL.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getterBuilder);
        }

        // Create setter if it exists
        var setter = property.GetSetMethod();
        if (setter != null)
        {
            var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                                 MethodAttributes.Virtual | MethodAttributes.NewSlot;

            var setterBuilder = typeBuilder.DefineMethod(
                setter.Name,
                methodAttributes,
                typeof(void),
                new[] { property.PropertyType });

            var setterIL = setterBuilder.GetILGenerator();

            // Load interceptor and call InterceptCall
            setterIL.Emit(OpCodes.Ldarg_0);
            setterIL.Emit(OpCodes.Ldfld, interceptorField);
            setterIL.Emit(OpCodes.Ldstr, setter.Name);
            setterIL.Emit(OpCodes.Ldc_I4_1);
            setterIL.Emit(OpCodes.Newarr, typeof(object));
            setterIL.Emit(OpCodes.Dup);
            setterIL.Emit(OpCodes.Ldc_I4_0);
            setterIL.Emit(OpCodes.Ldarg_1);
            if (property.PropertyType.IsValueType)
            {
                setterIL.Emit(OpCodes.Box, property.PropertyType);
            }
            setterIL.Emit(OpCodes.Stelem_Ref);

            var interceptCallMethod = typeof(ConcreteMethodInterceptor).GetMethod("InterceptCall");
            setterIL.Emit(OpCodes.Callvirt, interceptCallMethod);
            setterIL.Emit(OpCodes.Pop);
            setterIL.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setterBuilder);
        }
    }

    private static Type[] GetConstructorArgTypes(Type baseType)
    {
        var constructors = baseType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
        return primaryConstructor.GetParameters().Select(p => p.ParameterType).ToArray();
    }

    private static FieldBuilder DefineInterceptorField(TypeBuilder typeBuilder)
    {
        return typeBuilder.DefineField(
            "_interceptor",
            typeof(ConcreteMethodInterceptor),
            FieldAttributes.Private | FieldAttributes.InitOnly);
    }

    private static void DefineConstructor(TypeBuilder typeBuilder, Type baseType, FieldBuilder interceptorField, Type[] baseConstructorArgTypes)
    {
        var allArgTypes = baseConstructorArgTypes.Concat(new[] { typeof(ConcreteMethodInterceptor) }).ToArray();

        var constructorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            allArgTypes);

        var il = constructorBuilder.GetILGenerator();

        // Call base constructor
        il.Emit(OpCodes.Ldarg_0);
        for (int i = 1; i <= baseConstructorArgTypes.Length; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }

        var baseConstructor = baseType.GetConstructor(baseConstructorArgTypes);
        il.Emit(OpCodes.Call, baseConstructor);

        // Set interceptor field
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg, baseConstructorArgTypes.Length + 1);
        il.Emit(OpCodes.Stfld, interceptorField);
        il.Emit(OpCodes.Ret);
    }

    private static void DefineMethodProxy(TypeBuilder typeBuilder, MethodInfo method, FieldBuilder interceptorField)
    {
        var parameters = method.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

        var methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig |
                             MethodAttributes.Virtual | MethodAttributes.NewSlot;

        var methodBuilder = typeBuilder.DefineMethod(
            method.Name,
            methodAttributes,
            method.ReturnType,
            parameterTypes);

        // Copy parameter names and attributes
        for (int i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            methodBuilder.DefineParameter(i + 1, param.Attributes, param.Name);
        }

        var il = methodBuilder.GetILGenerator();

        // Load interceptor
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, interceptorField);

        // Load method name
        il.Emit(OpCodes.Ldstr, method.Name);

        // Create arguments array
        il.Emit(OpCodes.Ldc_I4, parameters.Length);
        il.Emit(OpCodes.Newarr, typeof(object));

        for (int i = 0; i < parameters.Length; i++)
        {
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4, i);
            il.Emit(OpCodes.Ldarg, i + 1);
            if (parameters[i].ParameterType.IsValueType)
            {
                il.Emit(OpCodes.Box, parameters[i].ParameterType);
            }
            il.Emit(OpCodes.Stelem_Ref);
        }

        // Call interceptor
        var interceptCallMethod = typeof(ConcreteMethodInterceptor).GetMethod("InterceptCall");
        il.Emit(OpCodes.Callvirt, interceptCallMethod);

        // Handle return value
        if (method.ReturnType != typeof(void))
        {
            if (method.ReturnType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, method.ReturnType);
            }
            else if (method.ReturnType != typeof(object))
            {
                il.Emit(OpCodes.Castclass, method.ReturnType);
            }
        }
        else
        {
            il.Emit(OpCodes.Pop);
        }

        il.Emit(OpCodes.Ret);
    }

    private static IEnumerable<MethodInfo> GetMethodsToProxy(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName &&
                       m.DeclaringType != typeof(object) &&
                       !m.IsFinal);
    }
}