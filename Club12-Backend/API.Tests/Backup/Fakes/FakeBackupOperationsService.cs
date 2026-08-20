using Application.DTOs.Backup.Response;
using Application.Interfaces.Backup;

using Domain.Enums;

namespace API.Tests.Backup.Fakes;

/// <summary>
/// Test double for IBackupOperationsService. Used both by
/// BackupControllerTests (configuring the exact outcome an endpoint
/// call should map to an HTTP status) and by
/// DatabaseBackupHostedServiceTests (using Gate/FailFirstCalls to
/// simulate a slow/failing scheduled attempt).
/// </summary>
public sealed class FakeBackupOperationsService : IBackupOperationsService
{
    private int _createCallCount;

    public int CreateCallCount => _createCallCount;

    /// <summary>
    /// When set, CreateBackupAsync awaits this before returning —
    /// simulates a still-running attempt for concurrency-related tests.
    /// </summary>
    public TaskCompletionSource<bool>? Gate { get; set; }

    /// <summary>
    /// The first N calls to CreateBackupAsync return
    /// Failed instead of NextCreateResult.
    /// </summary>
    public int FailFirstCalls { get; set; }

    public BackupOperationResult NextCreateResult { get; set; } =
        new(BackupOperationOutcome.Completed, new BackupRecordResponse(Guid.NewGuid(), DateTime.UtcNow, 0, "Manual", "fake.sql"), null);

    public BackupOperationResult NextDeleteResult { get; set; } =
        new(BackupOperationOutcome.Completed, null, null);

    public async Task<BackupOperationResult> CreateBackupAsync(BackupOrigin origin, CancellationToken ct = default)
    {
        int call = Interlocked.Increment(ref _createCallCount);

        if (Gate is not null)
        {
            await Gate.Task;
        }

        return call <= FailFirstCalls
            ? new BackupOperationResult(BackupOperationOutcome.Failed, null, $"Simulated failure on call {call}.")
            : NextCreateResult;
    }

    public Task<BackupOperationResult> DeleteBackupAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(NextDeleteResult);
    }
}
