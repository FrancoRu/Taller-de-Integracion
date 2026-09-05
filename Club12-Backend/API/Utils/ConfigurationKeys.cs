namespace API.Utils;

/// <summary>
/// Centralized configuration key paths read at startup, so a typo in a key name is caught by the compiler instead of silently returning null or a default value at runtime.
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
        /// When true, the startup DataSeeder deletes existing sample domain data before seeding, forcing a clean, FK-safe reseed; defaults to false.
        /// </summary>
        public const string Reset = "Seed:Reset";

        /// <summary>
        /// Filesystem folder the startup DataSeeder reads team crest PNGs from to upload as real team logos, falling back to placeholders when absent.
        /// </summary>
        public const string LogosPath = "Seed:LogosPath";

        /// <summary>
        /// Filesystem path to the medical PDF the startup DataSeeder uploads for every should-be-habilitado seeded registration, skipping the backfill step without failing the seed when absent.
        /// </summary>
        public const string MedicalRecordPath = "Seed:MedicalRecordPath";

        /// <summary>
        /// How many consecutive seasons of history the startup DataSeeder builds, counting backwards from the most recent one and clamped to a safe range by the seeder.
        /// </summary>
        public const string Seasons = "Seed:Seasons";

        /// <summary>
        /// Roster size for every seeded team, defaulting to SampleTournamentBuilder.DefaultPlayersPerTeam and clamped to a safe range by the seeder.
        /// </summary>
        public const string PlayersPerTeam = "Seed:PlayersPerTeam";
    }
}
