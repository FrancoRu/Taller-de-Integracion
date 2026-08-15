using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces.Backup;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for <see cref="IDatabaseBackupService"/>. Supports simulating
/// a slow/still-running dump (via <see cref="Gate"/>) for single-flight tests,
/// and simulating the first N calls failing (via <see cref="FailFirstCalls"/>)
/// for failure-isolation tests.
/// </summary>
public sealed class FakeDatabaseBackupService : IDatabaseBackupService
{
    private int _callCount;

    public int CallCount => _callCount;

    /// <summary>When set, <see cref="CreateDumpAsync"/> awaits this before returning/throwing.</summary>
    public TaskCompletionSource<bool>? Gate { get; set; }

    /// <summary>The first N calls throw <see cref="BackupExecutionException"/>; subsequent calls succeed.</summary>
    public int FailFirstCalls { get; set; }

    public string DumpContent { get; set; } = "-- fake dump --";

    public async Task<Stream> CreateDumpAsync(CancellationToken ct = default)
    {
        int call = Interlocked.Increment(ref _callCount);

        if (Gate is not null)
            await Gate.Task;

        if (call <= FailFirstCalls)
            throw new BackupExecutionException($"Simulated backup failure on call {call}.");

        return new MemoryStream(Encoding.UTF8.GetBytes(DumpContent));
    }
}
