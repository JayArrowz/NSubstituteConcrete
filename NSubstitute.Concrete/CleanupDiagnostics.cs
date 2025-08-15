namespace NSubstitute.Concrete;

/// <summary>
/// Diagnostic information for monitoring memory usage
/// </summary>
public class CleanupDiagnostics
{
    public int ActiveSubstituteCount { get; set; }
    public int CachedProxyTypeCount { get; set; }

    public override string ToString()
    {
        return $"Active Substitutes: {ActiveSubstituteCount}, Cached Proxy Types: {CachedProxyTypeCount}";
    }
}
