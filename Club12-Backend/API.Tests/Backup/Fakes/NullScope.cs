namespace API.Tests.Backup.Fakes;

/// <summary>
/// No-op IDisposable returned from ILogger.BeginScope, shared by every
/// CapturingLogger{T} instance regardless of T.
/// </summary>
public sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    public void Dispose()
    {
    }
}
