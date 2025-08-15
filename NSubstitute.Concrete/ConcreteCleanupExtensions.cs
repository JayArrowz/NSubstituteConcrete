namespace NSubstitute.Concrete;

/// <summary>
/// Extension methods for cleanup and resource management
/// </summary>
public static class ConcreteCleanupExtensions
{
    /// <summary>
    /// Remove a specific substitute from the interceptor registry
    /// </summary>
    public static void Cleanup<T>(this T substitute) where T : class
    {
        ConcreteExtensions.UnregisterInterceptor(substitute);
    }

    /// <summary>
    /// Clear all registered interceptors (useful for test cleanup)
    /// </summary>
    public static void ClearAllSubstitutes()
    {
        ConcreteExtensions.ClearAllInterceptors();
    }

    /// <summary>
    /// Clear both substitute registry AND proxy type cache
    /// Use this for complete cleanup in long-running applications
    /// </summary>
    public static void ClearAll()
    {
        ConcreteExtensions.ClearAllInterceptors();
        SubstituteExtensions.ClearProxyTypeCache();
    }

    /// <summary>
    /// Get reference count for a specific type
    /// </summary>
    public static int GetRefCount<T>() where T : class
    {
        return SubstituteExtensions.GetRefCount<T>();
    }

    /// <summary>
    /// Clear proxy type cache only (frees memory from generated types)
    /// </summary>
    public static void ClearProxyCache()
    {
        SubstituteExtensions.ClearProxyTypeCache();
    }

    /// <summary>
    /// Clear proxy cache for a specific type
    /// </summary>
    public static void ClearProxyCache<T>() where T : class
    {
        SubstituteExtensions.ClearProxyType<T>();
    }

    /// <summary>
    /// Get diagnostic information about memory usage
    /// </summary>
    public static CleanupDiagnostics GetDiagnostics()
    {
        return new CleanupDiagnostics
        {
            ActiveSubstituteCount = ConcreteExtensions.GetInterceptorCount(),
            CachedProxyTypeCount = SubstituteExtensions.GetProxyTypeCacheCount()
        };
    }
}
