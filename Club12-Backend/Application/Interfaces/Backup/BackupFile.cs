using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Metadata describing a single stored backup dump: its stored name and creation timestamp.
/// </summary>
public sealed record BackupFile(string Name, DateTimeOffset Timestamp);
