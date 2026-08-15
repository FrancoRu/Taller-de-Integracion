using System;
using System.Collections.Generic;
using System.Linq;
using Application.Interfaces.Backup;

namespace Application.Backup;

/// <summary>
/// Pure keep-last-N retention policy: retains the newest <c>retainCount</c>
/// entries (by <see cref="BackupFile.Timestamp"/>) and selects the rest for
/// deletion. No I/O.
/// </summary>
/// <remarks>
/// Tie-break rule (documented design decision, see design.md "Open
/// Questions"): when two or more entries share the exact same timestamp at
/// the retention boundary, entries are ordered newest-timestamp-first, then
/// by <see cref="BackupFile.Name"/> in ascending ordinal order. This makes
/// selection deterministic and independent of input order — the
/// lexically-smallest name among tied entries is preferred for retention.
/// </remarks>
public sealed class KeepLastNRetentionPolicy : IBackupRetentionPolicy
{
    public IReadOnlyList<BackupFile> SelectForDeletion(IReadOnlyList<BackupFile> existing, int retainCount)
    {
        ArgumentNullException.ThrowIfNull(existing);
        if (retainCount < 0)
            throw new ArgumentOutOfRangeException(nameof(retainCount), "Retention count cannot be negative.");

        if (existing.Count <= retainCount)
            return [];

        return existing
            .OrderByDescending(f => f.Timestamp)
            .ThenBy(f => f.Name, StringComparer.Ordinal)
            .Skip(retainCount)
            .ToList();
    }
}
