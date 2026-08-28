namespace Application.Interfaces.Backup;

/// <summary>
/// Outcome of a successful restore (HU-93), for the admin UI's confirmation.
/// <paramref name="RestoredFrom"/> is the backup that was replayed;
/// <paramref name="SafetyBackupName"/> is the safeguard that was taken before
/// restoring and then deleted on success. On failure the service throws
/// instead of returning this, and the safety backup is deliberately kept.
/// </summary>
public sealed record RestoreResult(string RestoredFrom, string SafetyBackupName);
