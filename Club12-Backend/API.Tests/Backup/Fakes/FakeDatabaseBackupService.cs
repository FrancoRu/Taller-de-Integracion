using Application.Interfaces.Backup;

using System.Text;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IDatabaseBackupService. Supports simulating
/// a slow/still-running dump (via Gate) for single-flight tests,
/// and simulating the first N calls failing (via FailFirstCalls)
/// for failure-isolation tests.
/// </summary>
public sealed class FakeDatabaseBackupService : IDatabaseBackupService
{
    private int _callCount;

    public int CallCount => _callCount;

    /// <summary>
    /// When set, CreateDumpAsync awaits this before returning/throwing.
    /// </summary>
    public TaskCompletionSource<bool>? Gate { get; set; }

    /// <summary>
    /// The first N calls throw BackupExecutionException; subsequent calls succeed.
    /// </summary>
    public int FailFirstCalls { get; set; }

    public string DumpContent { get; set; } = "-- fake dump --";

    public async Task<Stream> CreateDumpAsync(CancellationToken ct = default)
    {
        int call = Interlocked.Increment(ref _callCount);

        if (Gate is not null)
        {
            await Gate.Task;
        }

        return call <= FailFirstCalls
            ? throw new BackupExecutionException($"Simulated backup failure on call {call}.")
            : (Stream) new MemoryStream(Encoding.UTF8.GetBytes(DumpContent));
    }
}
