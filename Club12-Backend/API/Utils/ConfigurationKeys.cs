namespace API.Utils;

/// <summary>
/// Centralized configuration key paths read at startup, so a typo in a key
/// name is caught by the compiler instead of silently returning null or a
/// default value at runtime.
/// </summary>
public static class ConfigurationKeys
{
    public const string DbConnection = "DbConnection";
    public const string AllowedOrigins = "AllowedOrigins";
    public const string UseLoggingMiddleware = "UseLoggingMiddleware";

    public static class Jwt
    {
        public const string Key = "JWT:Key";
        public const string Issuer = "JWT:Issuer";
        public const string Audience = "JWT:Audience";
    }

    public static class Swagger
    {
        public const string Title = "Swagger:Title";
        public const string Version = "Swagger:Version";
    }

    public static class Smtp
    {
        public const string Host = "Smtp:Host";
        public const string Port = "Smtp:Port";
        public const string Username = "Smtp:Username";
        public const string Password = "Smtp:Password";
        public const string UseSsl = "Smtp:UseSsl";
        public const string FromEmail = "Smtp:FromEmail";
        public const string FromName = "Smtp:FromName";
    }

    public static class Backup
    {
        public const string Section = "Backup";
        public const string Enabled = "Backup:Enabled";
    }

    public static class Seed
    {
        public const string Enabled = "Seed:Enabled";

        /// <summary>
        /// When true, the startup DataSeeder deletes existing sample domain data
        /// (FK-safe) before seeding, forcing a clean reseed. Dev-only: the whole
        /// seed path is already gated by <see cref="Enabled"/>. Defaults to false.
        /// </summary>
        public const string Reset = "Seed:Reset";

        /// <summary>
        /// Filesystem folder the startup DataSeeder reads team crest PNGs from
        /// and uploads as real team logos. Absent/missing folder falls back to
        /// placeholder logos without failing the seed.
        /// </summary>
        public const string LogosPath = "Seed:LogosPath";

        /// <summary>
        /// Filesystem path to the medical PDF the startup DataSeeder uploads
        /// for every should-be-habilitado seeded registration
        /// (medical-records-storage-eligibility, Part 3). Absent/missing file
        /// warns and skips the whole backfill step without failing the seed.
        /// </summary>
        public const string MedicalRecordPath = "Seed:MedicalRecordPath";

        /// <summary>
        /// Bypasses the skip-if-teams-exist guard so the medical-records
        /// backfill step can run as a standalone targeted backfill against an
        /// already-seeded database, without a full <see cref="Reset"/> wipe.
        /// The step also runs during a normal reset seed regardless of this
        /// flag (medical-records-storage-eligibility, Part 3, ADR #8).
        /// </summary>
        public const string MedicalRecords = "Seed:MedicalRecords";
    }
}
