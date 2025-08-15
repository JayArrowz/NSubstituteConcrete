namespace NSubstitute.Concrete;

/// <summary>
/// Diagnostic information for monitoring memory usage with Harmony support
/// </summary>
public class CleanupDiagnostics
{
    public int ActiveSubstituteCount { get; set; }
    public int CachedProxyTypeCount { get; set; }

    /// <summary>
    /// Number of active Harmony interceptors
    /// </summary>
    public int HarmonyInterceptorCount => CachedProxyTypeCount;

    public override string ToString()
    {
        return $"Active Substitutes: {ActiveSubstituteCount}, Harmony Interceptors: {HarmonyInterceptorCount}";
    }
}