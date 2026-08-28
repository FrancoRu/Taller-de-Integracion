namespace Application.Interfaces.Services;

/// <summary>
/// Abstracts access to the current caller's identity so application services
/// can record "who" in the audit trail (HU-101) without depending on the web
/// layer. Implemented in the API using the HTTP context; resolves to the
/// system user when there is no authenticated request (background jobs,
/// tests, seeding).
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// The authenticated caller's identifier (email), or
    /// <see cref="Domain.Constants.AuditConstants.SystemUser"/> when no user
    /// is bound to the current execution context.
    /// </summary>
    string Actor { get; }
}
