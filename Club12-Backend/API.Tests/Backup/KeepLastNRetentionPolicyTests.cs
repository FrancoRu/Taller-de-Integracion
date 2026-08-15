using System;
using System.Collections.Generic;
using System.Linq;
using Application.Backup;
using Application.Interfaces.Backup;
using Xunit;

namespace API.Tests.Backup;

/// <summary>
/// Unit tests for the pure keep-last-N retention decision. No I/O — the
/// policy only selects which <see cref="BackupFile"/> entries to delete
/// given an already-known list and a retain count.
/// </summary>
public class KeepLastNRetentionPolicyTests
{
    private static readonly KeepLastNRetentionPolicy Policy = new();

    private static BackupFile File(string name, int minutesAgo) =>
        new(name, DateTimeOffset.UtcNow.AddMinutes(-minutesAgo));

    [Fact]
    public void SelectForDeletion_CountWithinLimit_SelectsNone()
    {
        List<BackupFile> existing =
        [
            File("backup-1", minutesAgo: 30),
            File("backup-2", minutesAgo: 20),
            File("backup-3", minutesAgo: 10),
        ];

        IReadOnlyList<BackupFile> result = Policy.SelectForDeletion(existing, retainCount: 5);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectForDeletion_CountEqualsLimit_SelectsNone()
    {
        List<BackupFile> existing =
        [
            File("backup-1", minutesAgo: 30),
            File("backup-2", minutesAgo: 20),
        ];

        IReadOnlyList<BackupFile> result = Policy.SelectForDeletion(existing, retainCount: 2);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectForDeletion_CountExceedsLimit_SelectsOldestExcess_RetainsNewestN()
    {
        // Oldest-to-newest by minutesAgo: backup-5 (50m) ... backup-1 (10m).
        List<BackupFile> existing =
        [
            File("backup-1", minutesAgo: 10),
            File("backup-2", minutesAgo: 20),
            File("backup-3", minutesAgo: 30),
            File("backup-4", minutesAgo: 40),
            File("backup-5", minutesAgo: 50),
        ];

        IReadOnlyList<BackupFile> result = Policy.SelectForDeletion(existing, retainCount: 2);

        Assert.Equal(3, result.Count);
        Assert.Equal(
            new[] { "backup-3", "backup-4", "backup-5" },
            result.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray());
        // Newest 2 (backup-1, backup-2) must be retained, i.e. absent from the deletion set.
        Assert.DoesNotContain(result, f => f.Name is "backup-1" or "backup-2");
    }

    [Fact]
    public void SelectForDeletion_IdenticalTimestampsAtBoundary_IsDeterministicAndOrderIndependent()
    {
        DateTimeOffset tiedTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10);

        List<BackupFile> existing =
        [
            new BackupFile("backup-b", tiedTimestamp),
            new BackupFile("backup-a", tiedTimestamp),
            new BackupFile("backup-c", tiedTimestamp),
        ];

        // Retain exactly 1 of the 3 tied entries — tie-break must be deterministic.
        IReadOnlyList<BackupFile> firstRun = Policy.SelectForDeletion(existing, retainCount: 1);

        // Same input, different list order — result must not depend on input order.
        List<BackupFile> reordered = [existing[2], existing[0], existing[1]];
        IReadOnlyList<BackupFile> secondRun = Policy.SelectForDeletion(reordered, retainCount: 1);

        Assert.Equal(2, firstRun.Count);
        Assert.Equal(
            firstRun.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal),
            secondRun.Select(f => f.Name).OrderBy(n => n, StringComparer.Ordinal));

        // Documented tie-break rule (design.md Open Questions): among entries with
        // identical timestamps, the lexically-smallest name (ordinal) is retained.
        Assert.DoesNotContain(firstRun, f => f.Name == "backup-a");
        Assert.Contains(firstRun, f => f.Name == "backup-b");
        Assert.Contains(firstRun, f => f.Name == "backup-c");
    }

    [Fact]
    public void SelectForDeletion_EmptyList_SelectsNone()
    {
        IReadOnlyList<BackupFile> result = Policy.SelectForDeletion([], retainCount: 7);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectForDeletion_RetainCountZero_SelectsAll()
    {
        List<BackupFile> existing =
        [
            File("backup-1", minutesAgo: 10),
            File("backup-2", minutesAgo: 20),
        ];

        IReadOnlyList<BackupFile> result = Policy.SelectForDeletion(existing, retainCount: 0);

        Assert.Equal(2, result.Count);
    }
}
