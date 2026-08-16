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
}
