namespace Application.Utils.Constants.Configuration;

/// <summary>
/// Centralized configuration key paths read directly by Application and Infrastructure code.
/// </summary>
public static class ConfigurationKeys
{
    /// <summary>
    /// Name of the connection string entry under ConnectionStrings, used for both the application database and the ASP.NET Core Identity database.
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
        /// Base URL of the account-activation page the invited user lands on from the invitation email.
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
        /// Name of the private Supabase bucket medical-record PDFs are stored in, separate from BucketName.
        /// </summary>
        public const string MedicalRecordsBucketName = "MedicalRecordsBucketName";
    }
}
