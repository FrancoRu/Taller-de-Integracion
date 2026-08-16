using System;

namespace Application.Interfaces.Backup;

/// <summary>
/// Metadata describing a single stored backup dump: its name (as stored —
/// e.g. object key or file name) and its creation timestamp.
/// </summary>
public sealed record BackupFile(string Name, DateTimeOffset Timestamp);
