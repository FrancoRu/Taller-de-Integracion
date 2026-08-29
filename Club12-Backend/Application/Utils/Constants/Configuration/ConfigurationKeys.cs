namespace Application.Utils.Constants.Configuration;

/// <summary>
/// Centralized configuration key paths read directly by Application and
/// Infrastructure code. ASP.NET Core startup wiring (Program.cs,
/// API/Utils/StartupExtensions.cs) has its own equivalent
/// API.Utils.ConfigurationKeys — that class lives in the API project, which
/// Application and Infrastructure cannot reference (Clean Architecture
/// dependency direction), so the handful of keys read outside startup are
/// duplicated here rather than shared directly.
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Name of the connection string entry (under "ConnectionStrings") used
    /// for both the application database and the ASP.NET Core Identity
    /// database.
    /// </summary>
    public const string DbConnection = "DbConnection";

    public static class Jwt
    {
        public const string Key = "JWT:Key";
        public const string Issuer = "JWT:Issuer";
        public const string Audience = "JWT:Audience";
    }

    public static class Frontend
    {
        public const string PasswordResetUrl = "Frontend:PasswordResetUrl";

        /// <summary>
        /// HU-09: base URL of the account-activation page the invited user lands
        /// on from the invitation email. Falls back to
        /// <see cref="PasswordResetUrl"/> when not configured, since both pages
        /// consume an Identity token in the same "?email=&amp;token=" shape.
        /// </summary>
        public const string ActivationUrl = "Frontend:ActivationUrl";
    }

    public static class AdminUser
    {
        public const string Email = "AdminUser:Email";
        public const string Password = "AdminUser:Password";
    }

    public static class Backup
    {
        public const string PgDumpPath = "Backup:PgDumpPath";
        public const string PsqlPath = "Backup:PsqlPath";
    }

    public static class Supabase
    {
        public const string Section = "SupaBase";
        public const string ProjectUrl = "ProjectUrl";
        public const string ServiceRole = "ServiceRole";
        public const string BucketName = "BucketName";

        /// <summary>
        /// Name of the private Supabase bucket medical-record PDFs are stored
        /// in, separate from <see cref="BucketName"/> (public-images). Falls
        /// back to <c>SupabaseMedicalRecordStorage.DefaultBucketName</c> when
        /// unset.
        /// </summary>
        public const string MedicalRecordsBucketName = "MedicalRecordsBucketName";
    }
}
