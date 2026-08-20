using API.Tests.Backup.Fakes;

using Application.Backup;
using Application.Interfaces.Backup;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for BackupOperationsService — the one shared write
/// path used by both BackupController (manual) and
/// DatabaseBackupHostedService (scheduled, via a DI scope). All
/// dependencies are fakes; no real pg_dump, storage I/O, or database is
/// involved. Covers backup-catalog#Single-Shared-Write-Path,
/// backup-catalog#Failed-Backups-Are-Not-Catalogued,
/// backup-catalog#Delete-Removes-Both-Stored-File-and-Catalog-Record, and
/// scheduled-database-backups#Keep-Last-N-Retention-Pruning (shared
/// Manual+Job pool).
/// </summary>
public class BackupOperationsServiceTests
{
    private static BackupOptions Options(int retentionCount = 7)
    {
        return new BackupOptions { RetentionCount = retentionCount };
    }

    private static BackupOperationsService CreateSut(
        FakeBackupCatalog? catalog = null,
        FakeBackupStorage? storage = null,
        FakeDatabaseBackupService? backupService = null,
        IBackupRetentionPolicy? retentionPolicy = null,
        BackupOptions? options = null,
        BackupOperationLock? operationLock = null,
        ILogger<BackupOperationsService>? logger = null)
    {
        return new BackupOperationsService(
            catalog ?? new FakeBackupCatalog(),
            storage ?? new FakeBackupStorage(),
            backupService ?? new FakeDatabaseBackupService(),
            retentionPolicy ?? new KeepLastNRetentionPolicy(),
            options ?? Options(),
            operationLock ?? new BackupOperationLock(),
            logger ?? NullLogger<BackupOperationsService>.Instance);
    }

    private static BackupRecord NewRecord(string storagePath, BackupOrigin origin, DateTime dateCreated)
    {
        return new BackupRecord
        {
            CreatedBy = AuditConstants.SystemUser,
            StoragePath = storagePath,
            SizeBytes = 1,
            Origin = origin,
            DateCreated = dateCreated,
        };
    }

    [Fact]
    public async Task CreateBackupAsync_Succeeds_AddsCatalogRecord()
    {
        FakeBackupCatalog catalog = new();
        BackupOperationsService sut = CreateSut(catalog: catalog);

        BackupOperationResult result = await sut.CreateBackupAsync(BackupOrigin.Manual);

        Assert.Equal(BackupOperationOutcome.Completed, result.Outcome);
        Assert.NotNull(result.Record);
        Assert.Equal("Manual", result.Record!.Origin);
        Assert.Equal(1, catalog.AddCallCount);
    }

    [Fact]
    public async Task CreateBackupAsync_ConcurrentCalls_SecondReturnsBusy_NoSecondCatalogRow()
    {
        FakeBackupCatalog catalog = new();
        FakeDatabaseBackupService backupService = new()
        {
            Gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        BackupOperationsService sut = CreateSut(catalog: catalog, backupService: backupService);

        Task<BackupOperationResult> firstCall = sut.CreateBackupAsync(BackupOrigin.Manual);
        bool startedFirstCall = await TestTiming.WaitUntilAsync(() => backupService.CallCount >= 1, TimeSpan.FromSeconds(2));
        Assert.True(startedFirstCall, "Expected the first call to reach the (gated) dump step before starting the second.");

        BackupOperationResult second = await sut.CreateBackupAsync(BackupOrigin.Job);

        backupService.Gate.SetResult(true);
        BackupOperationResult first = await firstCall;

        Assert.Equal(BackupOperationOutcome.Busy, second.Outcome);
        Assert.Equal(BackupOperationOutcome.Completed, first.Outcome);
        Assert.Equal(1, catalog.AddCallCount);
    }

    [Fact]
    public async Task CreateBackupAsync_DumpFails_NoCatalogRecordWritten()
    {
        FakeBackupCatalog catalog = new();
        FakeDatabaseBackupService backupService = new() { FailFirstCalls = 1 };
        BackupOperationsService sut = CreateSut(catalog: catalog, backupService: backupService);

        BackupOperationResult result = await sut.CreateBackupAsync(BackupOrigin.Manual);

        Assert.Equal(BackupOperationOutcome.Failed, result.Outcome);
        Assert.Equal(0, catalog.AddCallCount);
    }

    [Fact]
    public async Task DeleteBackupAsync_StorageFileMissing_CatalogRowStillRemoved_WarningLogged()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new() { DeleteException = new FileNotFoundException("missing") };
        CapturingLogger<BackupOperationsService> logger = new();
        BackupOperationsService sut = CreateSut(catalog: catalog, storage: storage, logger: logger);
        BackupRecord seeded = await catalog.AddAsync(NewRecord("backups/to-delete.sql", BackupOrigin.Manual, DateTime.UtcNow));

        BackupOperationResult result = await sut.DeleteBackupAsync(seeded.Id);

        Assert.Equal(BackupOperationOutcome.Completed, result.Outcome);
        Assert.Null(await catalog.GetByIdAsync(seeded.Id));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task DeleteBackupAsync_UnknownId_ReturnsNotFound()
    {
        BackupOperationsService sut = CreateSut();

        BackupOperationResult result = await sut.DeleteBackupAsync(Guid.NewGuid());

        Assert.Equal(BackupOperationOutcome.NotFound, result.Outcome);
    }

    /// <summary>
    /// scheduled-database-backups#Keep-Last-N-Retention-Pruning: retention
    /// now reads the catalog (Manual+Job combined), not
    /// IBackupStorage.ListAsync(), so pruning applies across both
    /// origins with no per-origin cap.
    /// </summary>
    [Fact]
    public async Task CreateBackupAsync_RetentionAppliesAcrossSharedManualAndJobPool_PrunesOldestRegardlessOfOrigin()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new();
        BackupOperationsService sut = CreateSut(catalog: catalog, storage: storage, options: Options(retentionCount: 2));

        DateTime baseline = DateTime.UtcNow.AddDays(-1);
        await catalog.AddAsync(NewRecord("job-old.sql", BackupOrigin.Job, baseline));
        await catalog.AddAsync(NewRecord("manual-mid.sql", BackupOrigin.Manual, baseline.AddHours(1)));

        BackupOperationResult result = await sut.CreateBackupAsync(BackupOrigin.Manual);

        Assert.Equal(BackupOperationOutcome.Completed, result.Outcome);
        IReadOnlyList<BackupRecord> remaining = await catalog.ListNewestFirstAsync();
        Assert.Equal(2, remaining.Count);
        Assert.DoesNotContain(remaining, r => r.StoragePath == "job-old.sql");
        Assert.Contains(remaining, r => r.StoragePath == "manual-mid.sql");
        Assert.Contains(storage.DeletedNames, n => n == "job-old.sql");
    }

    [Fact]
    public async Task CreateBackupAsync_WithinRetentionLimit_PrunesNothing()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new();
        BackupOperationsService sut = CreateSut(catalog: catalog, storage: storage, options: Options(retentionCount: 5));

        await catalog.AddAsync(NewRecord("job-old.sql", BackupOrigin.Job, DateTime.UtcNow.AddDays(-1)));

        await sut.CreateBackupAsync(BackupOrigin.Manual);

        IReadOnlyList<BackupRecord> remaining = await catalog.ListNewestFirstAsync();
        Assert.Equal(2, remaining.Count);
        Assert.Empty(storage.DeletedNames);
    }
}
