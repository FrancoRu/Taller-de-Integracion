namespace Domain.Constants;

/// <summary>
/// Identifies audit-trail entries (EntityBase.CreatedBy) created by an
/// automated process rather than an authenticated user — e.g. automated
/// stage or match generation.
/// </summary>
public static class AuditConstants
{
    public const string SystemUser = "System";
}
