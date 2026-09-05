namespace Application.Interfaces.Services;

/// <summary>
/// Abstracts access to the current caller's identity so application services can record who in the audit trail without depending on the web layer.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// The authenticated caller's identifier, their email, or AuditConstants.SystemUser when no user is bound to the current execution context.
    /// </summary>
    string Actor { get; }
}
