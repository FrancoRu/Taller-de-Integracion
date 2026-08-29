using API.Tests.Backup.Fakes;

using Application.Utils.Constants.Configuration;

using Infrastructure.Storage;

using Microsoft.Extensions.Configuration;

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace API.Tests;

/// <summary>
/// Covers Part 1 of the medical-records storage relocation: the object key
/// shape (<c>{teamId}/{playerId}/{guid}{ext}</c>, no <c>medical-records/</c>
/// prefix, no <c>tournamentId</c> segment), bucket routing to the configured
/// medical bucket (with a code-level default when unconfigured), and the
/// path-traversal threat-matrix guarantee that the two leading key segments
/// are exactly the caller-supplied Guids. <see cref="SupabaseBackupStorage"/>
/// and image-upload regression (default bucket preserved) live in
/// <c>SupabaseBackupStorageTests</c> and are asserted here only for the
/// bucket-parameterized raw boundary itself.
/// </summary>
public class MedicalRecordStorageTests
{
    private static IConfiguration EmptyConfiguration()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
    }

    private static IConfiguration ConfigurationWithBucket(string bucketName)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{ConfigurationKeys.Supabase.Section}:{ConfigurationKeys.Supabase.MedicalRecordsBucketName}"] = bucketName,
            })
            .Build();
    }

    [Fact]
    public async Task StoreAsync_KeyShape_IsTeamPlayerGuidExtension_NoLegacyPrefix()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseMedicalRecordStorage storage = new(raw, EmptyConfiguration());
        Guid teamId = Guid.NewGuid();
        Guid playerId = Guid.NewGuid();

        string objectPath = await storage.StoreAsync(teamId, playerId, "ficha.pdf", ContentStream("%PDF-1.4"));

        Assert.DoesNotContain("medical-records/", objectPath);
        string[] segments = objectPath.Split('/');
        Assert.Equal(3, segments.Length);
        Assert.Equal(teamId.ToString(), segments[0]);
        Assert.Equal(playerId.ToString(), segments[1]);
        Assert.EndsWith(".pdf", segments[2]);
    }

    [Fact]
    public async Task StoreAsync_DifferentTeamAndPlayer_TeamIdIsAlwaysFirstSegment()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseMedicalRecordStorage storage = new(raw, EmptyConfiguration());
        Guid teamId = Guid.NewGuid();
        Guid playerId = Guid.NewGuid();

        string objectPath = await storage.StoreAsync(teamId, playerId, "otra.pdf", ContentStream("%PDF-1.4 b"));

        Assert.StartsWith($"{teamId}/{playerId}/", objectPath);
    }

    [Fact]
    public async Task StoreAsync_NoConfiguredBucket_FallsBackToDefaultMedicalRecordsBucket()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseMedicalRecordStorage storage = new(raw, EmptyConfiguration());

        await storage.StoreAsync(Guid.NewGuid(), Guid.NewGuid(), "ficha.pdf", ContentStream("%PDF-1.4"));

        Assert.Single(raw.UploadedBuckets);
        Assert.Equal(SupabaseMedicalRecordStorage.DefaultBucketName, raw.UploadedBuckets[0]);
    }

    [Fact]
    public async Task StoreAsync_ConfiguredBucket_IsPassedExplicitly()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseMedicalRecordStorage storage = new(raw, ConfigurationWithBucket("medical-records-staging"));

        await storage.StoreAsync(Guid.NewGuid(), Guid.NewGuid(), "ficha.pdf", ContentStream("%PDF-1.4"));

        Assert.Equal("medical-records-staging", raw.UploadedBuckets[0]);
    }

    [Fact]
    public async Task DownloadAsync_TargetsTheConfiguredMedicalBucket()
    {
        FakeSupabaseRawStorage raw = new() { BytesToDownload = Encoding.UTF8.GetBytes("%PDF-1.4 body") };
        SupabaseMedicalRecordStorage storage = new(raw, ConfigurationWithBucket("medical-records-staging"));

        byte[] downloaded = await storage.DownloadAsync("team/player/guid.pdf");

        Assert.Equal("medical-records-staging", raw.DownloadedBucket);
        Assert.Equal(Encoding.UTF8.GetBytes("%PDF-1.4 body"), downloaded);
    }

    // ---------- path-traversal threat-matrix (RED) ----------

    [Fact]
    public async Task StoreAsync_KeySegments_ParseBackAsTheSuppliedGuids()
    {
        FakeSupabaseRawStorage raw = new();
        SupabaseMedicalRecordStorage storage = new(raw, EmptyConfiguration());
        Guid teamId = Guid.NewGuid();
        Guid playerId = Guid.NewGuid();

        string objectPath = await storage.StoreAsync(teamId, playerId, "ficha.pdf", ContentStream("%PDF-1.4"));

        string[] segments = objectPath.Split('/');
        Assert.True(Guid.TryParse(segments[0], out Guid parsedTeamId));
        Assert.True(Guid.TryParse(segments[1], out Guid parsedPlayerId));
        Assert.Equal(teamId, parsedTeamId);
        Assert.Equal(playerId, parsedPlayerId);
    }

    // ---------- backup/image regression: default bucket preserved ----------

    [Fact]
    public async Task BackupStorage_StillPassesNullBucket_DefaultBucketPreserved()
    {
        FakeSupabaseRawStorage raw = new();
        Infrastructure.Backup.SupabaseBackupStorage backupStorage = new(raw);

        await backupStorage.StoreAsync("backup-1.sql", ContentStream("dump"));

        Assert.Single(raw.UploadedBuckets);
        Assert.Null(raw.UploadedBuckets[0]);
    }

    private static Stream ContentStream(string text)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }
}
