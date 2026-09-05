using Domain.Enums;

namespace Domain.Entities.Models;

/// <summary>
/// An immutable audit-trail entry recording a sensitive action, since the shared owner and admin accounts are otherwise indistinguishable.
/// </summary>
public class AuditLog : EntityBase
{
    /// <summary>
    /// The sensitive action that was performed.
    /// </summary>
    public required AuditAction Action { get; set; }

    /// <summary>
    /// Who performed the action — the authenticated caller's email, or AuditConstants.SystemUser for an automated process with no HTTP identity.
    /// </summary>
    public required string Actor { get; set; }

    /// <summary>
    /// The kind of entity the action targeted, or null when the action is not tied to a single entity.
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Identifier of the targeted entity, when applicable.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// The target's human-readable name captured at write time, since the target may later be renamed or deleted.
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// Free-form human-readable context describing the action in detail.
    /// </summary>
    public string? Detail { get; set; }

    // Timestamp is EntityBase.DateCreated in UTC.
}
