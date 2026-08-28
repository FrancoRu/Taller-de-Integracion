using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace API.Tests.Backup;

/// <summary>
/// Verifies EfBackupCatalog's CRUD surface (IBackupCatalog) against a real
/// EF Core context backed by SQLite in-memory, via
/// CustomWebApplicationFactory (same harness used by
/// DataMaintenanceServiceTests). Covers spec
/// backup-catalog#Catalog-Powers-the-Admin-Backup-Listing: AddAsync
/// persists a record and assigns an Id, GetByIdAsync resolves a single
/// record (or null when missing), ListNewestFirstAsync orders the full
/// catalog by creation date descending, and RemoveAsync deletes a record
/// and is idempotent when the id is already gone.
/// </summary>
public sealed class EfBackupCatalogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public EfBackupCatalogTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static BackupRecord NewRecord(string storagePath, BackupOrigin origin, DateTime dateCreated)
    {
        return new BackupRecord
        {
            CreatedBy = AuditConstants.SystemUser,
            StoragePath = storagePath,
            SizeBytes = 1024,
            Origin = origin,
            DateCreated = dateCreated,
        };
    }

    [Fact]
    public async Task AddAsync_PersistsRecord_AndAssignsId()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);

        BackupRecord record = NewRecord("backups/manual-1.sql", BackupOrigin.Manual, DateTime.UtcNow);

        BackupRecord added = await catalog.AddAsync(record);

        Assert.NotEqual(Guid.Empty, added.Id);

        BackupRecord? persisted = await db.BackupRecords.FindAsync(added.Id);
        Assert.NotNull(persisted);
        Assert.Equal("backups/manual-1.sql", persisted!.StoragePath);
        Assert.Equal(BackupOrigin.Manual, persisted.Origin);
        Assert.Equal(1024, persisted.SizeBytes);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingRecord()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);

        BackupRecord added = await catalog.AddAsync(NewRecord("backups/job-1.sql", BackupOrigin.Job, DateTime.UtcNow));

        BackupRecord? found = await catalog.GetByIdAsync(added.Id);

        Assert.NotNull(found);
        Assert.Equal(added.Id, found!.Id);
        Assert.Equal(BackupOrigin.Job, found.Origin);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenRecordDoesNotExist()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);

        BackupRecord? found = await catalog.GetByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public async Task ListNewestFirstAsync_OrdersRecordsByDateCreatedDescending()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);

        // xUnit does not guarantee fact execution order and
        // CustomWebApplicationFactory shares one database per class, so
        // earlier facts in this class may have left rows behind — establish
        // a known-empty fixture before asserting exact ordering.
        List<BackupRecord> existing = await db.BackupRecords.ToListAsync();
        db.BackupRecords.RemoveRange(existing);
        await db.SaveChangesAsync();

        DateTime baseline = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        BackupRecord oldest = await catalog.AddAsync(NewRecord("backups/oldest.sql", BackupOrigin.Job, baseline));
        BackupRecord middle = await catalog.AddAsync(NewRecord("backups/middle.sql", BackupOrigin.Manual, baseline.AddHours(1)));
        BackupRecord newest = await catalog.AddAsync(NewRecord("backups/newest.sql", BackupOrigin.Job, baseline.AddHours(2)));

        IReadOnlyList<BackupRecord> result = await catalog.ListNewestFirstAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(middle.Id, result[1].Id);
        Assert.Equal(oldest.Id, result[2].Id);
    }

    [Fact]
    public async Task RemoveAsync_DeletesRecord()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);

        BackupRecord added = await catalog.AddAsync(NewRecord("backups/to-delete.sql", BackupOrigin.Manual, DateTime.UtcNow));

        await catalog.RemoveAsync(added.Id);

        BackupRecord? persisted = await db.BackupRecords.FindAsync(added.Id);
        Assert.Null(persisted);
    }

    [Fact]
    public async Task RemoveAsync_IsIdempotent_WhenRecordAlreadyMissing()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
        EfBackupCatalog catalog = new(db);
        Guid missingId = Guid.NewGuid();

        // Should not throw even though nothing exists at this id.
        await catalog.RemoveAsync(missingId);

        BackupRecord? stillMissing = await catalog.GetByIdAsync(missingId);
        Assert.Null(stillMissing);
    }
}
