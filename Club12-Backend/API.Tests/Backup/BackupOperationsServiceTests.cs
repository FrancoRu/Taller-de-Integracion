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
        FakeDatabaseRestoreService? restoreService = null,
        IBackupRetentionPolicy? retentionPolicy = null,
        BackupOptions? options = null,
        BackupOperationLock? operationLock = null,
        IMaintenanceModeState? maintenanceModeState = null,
        FakeAuditService? auditService = null,
        ILogger<BackupOperationsService>? logger = null)
    {
        return new BackupOperationsService(
            catalog ?? new FakeBackupCatalog(),
            storage ?? new FakeBackupStorage(),
            backupService ?? new FakeDatabaseBackupService(),
            restoreService ?? new FakeDatabaseRestoreService(),
            retentionPolicy ?? new KeepLastNRetentionPolicy(),
            options ?? Options(),
            operationLock ?? new BackupOperationLock(),
            maintenanceModeState ?? new MaintenanceModeState(),
            auditService ?? new FakeAuditService(),
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

    /// <summary>
    /// database-restore#Automatic-Pre-Restore-Safety-Backup: every restore
    /// takes an automatic backup of the current state first, catalogued with
    /// Origin = Job and applyRetention: false — so it is kept
    /// even if the catalog is already at (or past) RetentionCount.
    /// </summary>
    [Fact]
    public async Task RestoreBackupAsync_TakesSafetyBackupWithJobOriginAndNoRetention_EvenPastRetentionLimit()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new();
        MaintenanceModeState maintenanceModeState = new();
        BackupOperationsService sut = CreateSut(
            catalog: catalog,
            storage: storage,
            options: Options(retentionCount: 1),
            maintenanceModeState: maintenanceModeState);
        BackupRecord target = await catalog.AddAsync(NewRecord("existing.sql", BackupOrigin.Manual, DateTime.UtcNow.AddDays(-1)));

        BackupOperationResult result = await sut.RestoreBackupAsync(target.Id);

        Assert.Equal(BackupOperationOutcome.Completed, result.Outcome);
        Assert.NotNull(result.Record);
        Assert.Equal("Job", result.Record!.Origin);

        // RetentionCount is 1, but the safety backup uses applyRetention: false,
        // so both the pre-existing target AND the new safety backup survive.
        IReadOnlyList<BackupRecord> all = await catalog.ListNewestFirstAsync();
        Assert.Equal(2, all.Count);
        BackupRecord safety = Assert.Single(all, r => r.Id != target.Id);
        Assert.Equal(BackupOrigin.Job, safety.Origin);
        Assert.False(maintenanceModeState.IsActive);
    }

    /// <summary>
    /// HU-101: a successful restore must be auditable — AuditAction.BackupRestore
    /// existed in the enum but nothing ever logged it (a real gap found while
    /// auditing historias-de-usuario.md against the actual code).
    /// </summary>
    [Fact]
    public async Task RestoreBackupAsync_Succeeds_LogsBackupRestoreAuditEntry()
    {
        FakeBackupCatalog catalog = new();
        FakeAuditService auditService = new();
        BackupOperationsService sut = CreateSut(catalog: catalog, auditService: auditService);
        BackupRecord target = await catalog.AddAsync(NewRecord("existing.sql", BackupOrigin.Manual, DateTime.UtcNow));

        BackupOperationResult result = await sut.RestoreBackupAsync(target.Id);

        Assert.Equal(BackupOperationOutcome.Completed, result.Outcome);
        Assert.Contains(AuditAction.BackupRestore, auditService.LoggedActions);
    }

    [Fact]
    public async Task RestoreBackupAsync_UnknownId_ReturnsNotFound()
    {
        BackupOperationsService sut = CreateSut();

        BackupOperationResult result = await sut.RestoreBackupAsync(Guid.NewGuid());

        Assert.Equal(BackupOperationOutcome.NotFound, result.Outcome);
    }

    /// <summary>
    /// database-restore#Restore-Failure-Is-Logged-and-Isolated +
    /// threat-matrix "Temp-file handling during restore": a restore failure
    /// must still clear maintenance mode, delete the temp dump file, and
    /// never throw out of RestoreBackupAsync (no host crash).
    /// </summary>
    [Fact]
    public async Task RestoreBackupAsync_RestoreServiceThrows_MaintenanceExited_TempFileDeleted_NoCrash()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new();
        FakeDatabaseRestoreService restoreService = new() { ExceptionToThrow = new BackupExecutionException("psql failed") };
        MaintenanceModeState maintenanceModeState = new();
        BackupOperationsService sut = CreateSut(
            catalog: catalog,
            storage: storage,
            restoreService: restoreService,
            maintenanceModeState: maintenanceModeState);
        BackupRecord target = await catalog.AddAsync(NewRecord("existing.sql", BackupOrigin.Manual, DateTime.UtcNow));

        BackupOperationResult result = await sut.RestoreBackupAsync(target.Id);

        Assert.Equal(BackupOperationOutcome.Failed, result.Outcome);
        Assert.False(maintenanceModeState.IsActive);
        Assert.NotNull(restoreService.CapturedDumpFilePath);
        Assert.False(File.Exists(restoreService.CapturedDumpFilePath));
    }

    /// <summary>
    /// threat-matrix "Denial of service via repeated restore": the same
    /// single-flight BackupOperationLock used by create/delete also guards
    /// restore, so a concurrent restore attempt returns Busy (409 at the
    /// controller) instead of running alongside another restore.
    /// </summary>
    [Fact]
    public async Task RestoreBackupAsync_ConcurrentCalls_SecondReturnsBusy_OnlyOneRestoreRuns()
    {
        FakeBackupCatalog catalog = new();
        FakeBackupStorage storage = new();
        FakeDatabaseBackupService backupService = new()
        {
            Gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        FakeDatabaseRestoreService restoreService = new();
        BackupOperationsService sut = CreateSut(
            catalog: catalog, storage: storage, backupService: backupService, restoreService: restoreService);
        BackupRecord target = await catalog.AddAsync(NewRecord("existing.sql", BackupOrigin.Manual, DateTime.UtcNow));

        Task<BackupOperationResult> firstCall = sut.RestoreBackupAsync(target.Id);
        bool startedFirstCall = await TestTiming.WaitUntilAsync(() => backupService.CallCount >= 1, TimeSpan.FromSeconds(2));
        Assert.True(startedFirstCall, "Expected the first restore to reach the (gated) safety-backup dump step before starting the second.");

        BackupOperationResult second = await sut.RestoreBackupAsync(target.Id);

        backupService.Gate.SetResult(true);
        BackupOperationResult first = await firstCall;

        Assert.Equal(BackupOperationOutcome.Busy, second.Outcome);
        Assert.Equal(BackupOperationOutcome.Completed, first.Outcome);
        Assert.Equal(1, restoreService.CallCount);
    }
}
