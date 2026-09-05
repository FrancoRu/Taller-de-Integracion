using Application.Interfaces.Backup;
using Application.Utils.Constants;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Backup;

/// <summary>
/// Pure keep-last-N retention policy that retains the newest entries by timestamp with no I/O.
/// </summary>
public sealed class KeepLastNRetentionPolicy : IBackupRetentionPolicy
{
    public IReadOnlyList<BackupFile> SelectForDeletion(IReadOnlyList<BackupFile> existing, int retainCount)
    {
        ArgumentNullException.ThrowIfNull(existing);

        if (retainCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retainCount), ErrorMessages.Backup.RetentionCountNegative);
        }

        if (existing.Count <= retainCount)
        {
            return [];
        }

        return existing
            .OrderByDescending(f => f.Timestamp)
            .ThenBy(f => f.Name, StringComparer.Ordinal)
            .Skip(retainCount)
            .ToList();
    }
}
