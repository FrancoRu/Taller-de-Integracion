namespace Application.Interfaces.Backup;

/// <summary>
/// Named <see cref="BackupOptions.StorageTarget"/> values. Any value other
/// than <see cref="Supabase"/> (case-insensitively) resolves to the local
/// storage backend — including an unset or unrecognized configuration
/// value — so a typo in configuration fails safe rather than crashing the
/// host at startup.
/// </summary>
public static class BackupStorageTargets
{
    public const string Local = "Local";
    public const string Supabase = "Supabase";
}
