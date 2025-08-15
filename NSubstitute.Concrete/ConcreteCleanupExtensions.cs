namespace NSubstitute.Concrete;

/// <summary>
/// Extension methods for cleanup and resource management with Harmony support
/// </summary>
public static class ConcreteCleanupExtensions
{
    /// <summary>
    /// Remove a specific substitute from the interceptor registry and cleanup Harmony patches
    /// </summary>
    public static void Cleanup<T>(this T substitute) where T : class
    {
        ConcreteExtensions.UnregisterInterceptor(substitute);
    }

    /// <summary>
    /// Clear all registered interceptors and Harmony patches (useful for test cleanup)
    /// </summary>
    public static void ClearAllSubstitutes()
    {
        ConcreteExtensions.ClearAllInterceptors();
    }

    /// <summary>
    /// Complete cleanup - clears all interceptors and Harmony patches
    /// Use this for complete cleanup in long-running applications
    /// </summary>
    public static void ClearAll()
    {
        ConcreteExtensions.ClearAllInterceptors();
    }

    /// <summary>
    /// Get diagnostic information about memory usage
    /// </summary>
    public static CleanupDiagnostics GetDiagnostics()
    {
        return new CleanupDiagnostics
        {
            ActiveSubstituteCount = ConcreteExtensions.GetInterceptorCount(),
            CachedProxyTypeCount = SubstituteExtensions.GetInterceptorCount() // Harmony interceptors
        };
    }

    /// <summary>
    /// Force cleanup of all Harmony patches (use sparingly)
    /// </summary>
    public static void ClearAllHarmonyPatches()
    {
        SubstituteExtensions.ClearAllInterceptors();
    }
}