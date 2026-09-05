namespace Application.Interfaces.Backup;

/// <summary>
/// Named BackupOptions.StorageTarget values, where any value other than Supabase resolves to the local storage backend so a configuration typo fails safe.
/// </summary>
public static class BackupStorageTargets
{
    public const string Local = "Local";
    public const string Supabase = "Supabase";
}
