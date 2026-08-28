using Application.Interfaces.Backup;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IDatabaseRestoreService (the low-level psql apply). Records
/// the content it was handed and how many times it ran, can be told to fail
/// (simulating a non-zero psql exit), and exposes an OnRestore hook so a test
/// can assert what state the storage was in at the moment the restore ran
/// (e.g. that the safety backup already existed).
/// </summary>
public sealed class FakeDatabaseRestoreService : IDatabaseRestoreService
{
    public int CallCount { get; private set; }
    public string? CapturedContent { get; private set; }

    /// <summary>When true, RestoreAsync throws a BackupExecutionException.</summary>
    public bool ShouldThrow { get; set; }

    /// <summary>Runs just before the (optional) failure, after capturing content.</summary>
    public Func<Task>? OnRestore { get; set; }

    public async Task RestoreAsync(Stream dumpContent, CancellationToken ct = default)
    {
        CallCount++;
        using StreamReader reader = new(dumpContent);
        CapturedContent = await reader.ReadToEndAsync(ct);

        if (OnRestore is not null)
        {
            await OnRestore();
        }

        if (ShouldThrow)
        {
            throw new BackupExecutionException("Simulated restore failure (non-zero psql exit).");
        }
    }
}
