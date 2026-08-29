using Application.Interfaces.Storage;

using Domain.Entities.Models;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using System.IO;
using System.Threading;

namespace API.Tests;

/// <summary>
/// Covers Part 3 of medical-records-storage-eligibility: the gated, idempotent,
/// failure-tolerant <c>DataSeeder.SeedMedicalRecordsAsync</c> backfill step.
/// Exercised through the public <c>Seed:MedicalRecords=true</c> standalone
/// backfill entry point (<c>SeedAsync(reset: false, ..., forceMedicalRecords:
/// true)</c> against a DB that already has a team) — this is the same
/// bypass-the-skip-guard path a real "already seeded" environment takes
/// (ADR #8), and it reaches the private step without reflection. Constructed
/// with a <see langword="null!"/> <c>SupabaseHelper</c> because the step under
/// test never touches it (only <see cref="IMedicalRecordStorage"/>) — the
/// same documented pattern as <c>SupabaseDependentControllerNotFoundTests</c>
/// for this project's live-Supabase-constructor testability gap.
/// </summary>
public class MedicalRecordSeedTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public MedicalRecordSeedTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeedMedicalRecords_NullReference_IsUploaded()
    {
        using TempPdfFile pdf = new();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration = await SeedApprovedRegistrationAsync(db, fileUrl: null);
        FakeMedicalRecordStorage storage = new();

        await RunSeedMedicalRecordsAsync(db, storage, pdf.Path);

        // Scoped to this test's own (TeamId, PlayerId): the shared class-level
        // fixture's DB persists rows across test methods, so asserting on the
        // storage's global call count would be flaky under xUnit's shared
        // IClassFixture instance — assert this test's own row was uploaded.
        Assert.Contains((registration.TeamId, registration.PlayerId), storage.StoredCalls);
        PlayerTeamRegistration? reloaded = await db.PlayerTeamRegistrations
            .AsNoTracking().SingleAsync(r => r.Id == registration.Id);
        Assert.NotNull(reloaded!.MedicalRecordFileUrl);
        Assert.StartsWith($"{registration.TeamId}/{registration.PlayerId}/", reloaded.MedicalRecordFileUrl);
    }

    [Fact]
    public async Task SeedMedicalRecords_LegacyReference_IsUploaded()
    {
        using TempPdfFile pdf = new();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration =
            await SeedApprovedRegistrationAsync(db, fileUrl: "medical-records/some/object/path.pdf");
        FakeMedicalRecordStorage storage = new();

        await RunSeedMedicalRecordsAsync(db, storage, pdf.Path);

        Assert.Contains((registration.TeamId, registration.PlayerId), storage.StoredCalls);
    }

    [Fact]
    public async Task SeedMedicalRecords_NewSchemeReference_IsSkipped()
    {
        using TempPdfFile pdf = new();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration = await SeedApprovedRegistrationAsync(db, fileUrl: null);
        registration.MedicalRecordFileUrl = $"{registration.TeamId}/{registration.PlayerId}/{Guid.NewGuid()}.pdf";
        await db.SaveChangesAsync();

        FakeMedicalRecordStorage storage = new();

        await RunSeedMedicalRecordsAsync(db, storage, pdf.Path);

        Assert.DoesNotContain((registration.TeamId, registration.PlayerId), storage.StoredCalls);
    }

    [Fact]
    public async Task SeedMedicalRecords_SecondRun_UploadsZero()
    {
        using TempPdfFile pdf = new();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration = await SeedApprovedRegistrationAsync(db, fileUrl: null);
        FakeMedicalRecordStorage firstRunStorage = new();
        await RunSeedMedicalRecordsAsync(db, firstRunStorage, pdf.Path);
        Assert.Contains((registration.TeamId, registration.PlayerId), firstRunStorage.StoredCalls);

        FakeMedicalRecordStorage secondRunStorage = new();
        await RunSeedMedicalRecordsAsync(db, secondRunStorage, pdf.Path);

        Assert.DoesNotContain((registration.TeamId, registration.PlayerId), secondRunStorage.StoredCalls);
    }

    [Fact]
    public async Task SeedMedicalRecords_UploadThrows_DoesNotFailTheSeed_AndLeavesRefNull()
    {
        using TempPdfFile pdf = new();
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration = await SeedApprovedRegistrationAsync(db, fileUrl: null);
        FakeMedicalRecordStorage storage = new() { ExceptionToThrow = new InvalidOperationException("network unreachable") };

        // Must complete without throwing.
        await RunSeedMedicalRecordsAsync(db, storage, pdf.Path);

        PlayerTeamRegistration? reloaded = await db.PlayerTeamRegistrations
            .AsNoTracking().SingleAsync(r => r.Id == registration.Id);
        Assert.Null(reloaded!.MedicalRecordFileUrl);
    }

    [Fact]
    public async Task SeedMedicalRecords_MissingPdfPath_WarnsAndSkips_ZeroUploads()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDBContext db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

        PlayerTeamRegistration registration = await SeedApprovedRegistrationAsync(db, fileUrl: null);
        FakeMedicalRecordStorage storage = new();
        string missingPath = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid()}.pdf");

        await RunSeedMedicalRecordsAsync(db, storage, missingPath);

        Assert.DoesNotContain((registration.TeamId, registration.PlayerId), storage.StoredCalls);
        PlayerTeamRegistration? reloaded = await db.PlayerTeamRegistrations
            .AsNoTracking().SingleAsync(r => r.Id == registration.Id);
        Assert.Null(reloaded!.MedicalRecordFileUrl);
    }

    // ---------- helpers ----------

    private static async Task RunSeedMedicalRecordsAsync(
        ApplicationDBContext db, IMedicalRecordStorage storage, string medicalRecordPath)
    {
        // A team must already exist for the standalone-backfill bypass branch
        // (Seed:MedicalRecords=true) to short-circuit into SeedMedicalRecordsAsync
        // instead of running the full sample reseed (ADR #8).
        if (!await db.Teams.AnyAsync())
        {
            db.Teams.Add(new Team
            {
                Name = $"Seed-marker-{Guid.NewGuid()}",
                Slug = $"seed-marker-{Guid.NewGuid()}",
                ThreeLetterCode = "SDM",
                LogoUrl = "https://example.test/logo.png",
                ShirtColor = "Black",
                Players = [],
                CreatedBy = "test",
            });
            await db.SaveChangesAsync();
        }

        DataSeeder seeder = new(db, NullLogger<DataSeeder>.Instance, null!, storage);
        await seeder.SeedAsync(reset: false, medicalRecordPath: medicalRecordPath, forceMedicalRecords: true);
    }

    private static async Task<PlayerTeamRegistration> SeedApprovedRegistrationAsync(
        ApplicationDBContext db, string? fileUrl)
    {
        Tournament tournament = new()
        {
            Description = "Seed test tournament",
            Name = $"Tournament-{Guid.NewGuid()}",
            Slug = $"tournament-{Guid.NewGuid()}",
            TeamRegistrationDeadline = DateTime.UtcNow.Date.AddDays(29),
            StartDate = DateTime.UtcNow.Date.AddDays(30),
            Divisions = [],
            Teams = [],
            CreatedBy = "test",
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        Team team = new()
        {
            Name = $"Team-{Guid.NewGuid()}",
            Slug = $"team-{Guid.NewGuid()}",
            ThreeLetterCode = Guid.NewGuid().ToString("N")[..3].ToUpperInvariant(),
            LogoUrl = "https://example.test/logo.png",
            ShirtColor = "Blue",
            TournamentId = tournament.Id,
            Players = [],
            CreatedBy = "test",
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();

        Player player = new()
        {
            Slug = $"player-{Guid.NewGuid()}",
            FirstName = "Test",
            LastName = $"Player-{Guid.NewGuid():N}",
            DocumentNumber = Guid.NewGuid().ToString("N")[..10],
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = team,
            TeamId = team.Id,
            CreatedBy = "test",
        };
        db.Players.Add(player);
        await db.SaveChangesAsync();

        PlayerTeamRegistration registration = new()
        {
            PlayerId = player.Id,
            TeamId = team.Id,
            TournamentId = tournament.Id,
            MedicalRecordStatus = MedicalRecordStatus.Approved,
            MedicalRecordFileUrl = fileUrl,
            CreatedBy = "test",
        };
        db.PlayerTeamRegistrations.Add(registration);
        await db.SaveChangesAsync();

        return registration;
    }

    private sealed class FakeMedicalRecordStorage : IMedicalRecordStorage
    {
        public List<(Guid TeamId, Guid PlayerId)> StoredCalls { get; } = [];

        public Exception? ExceptionToThrow { get; set; }

        public Task<string> StoreAsync(
            Guid teamId, Guid playerId, string fileName, Stream content, CancellationToken ct = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            StoredCalls.Add((teamId, playerId));
            return Task.FromResult($"{teamId}/{playerId}/{Guid.NewGuid()}{Path.GetExtension(fileName)}");
        }

        public Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    /// <summary>A small real file on disk so <c>File.Exists</c>/<c>ReadAllBytesAsync</c> succeed.</summary>
    private sealed class TempPdfFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"seed-test-{Guid.NewGuid()}.pdf");

        public TempPdfFile()
        {
            File.WriteAllBytes(Path, "%PDF-1.4 fake seed medical record"u8.ToArray());
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
