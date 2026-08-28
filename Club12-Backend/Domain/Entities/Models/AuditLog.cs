using Domain.Enums;

namespace Domain.Entities.Models;

/// <summary>
/// An immutable audit-trail entry recording a sensitive action (HU-101):
/// who did it, what was done, on which target, when, and an optional detail.
/// The two shared owner/admin accounts make this trail the only way to tell
/// the operators apart, so entries are written for data wipes, backup
/// restores, tournament status changes and password resets/blanqueos.
/// </summary>
public class AuditLog : EntityBase
{
    /// <summary>The sensitive action that was performed.</summary>
    public required AuditAction Action { get; set; }

    /// <summary>
    /// Who performed the action — the authenticated caller's email, or
    /// <see cref="Domain.Constants.AuditConstants.SystemUser"/> when triggered
    /// by an automated/background process with no HTTP identity.
    /// </summary>
    public required string Actor { get; set; }

    /// <summary>
    /// The kind of entity the action targeted (e.g. "Tournament", "User"),
    /// or null for actions that are not tied to a single entity (e.g. a wipe).
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>Identifier of the targeted entity, when applicable.</summary>
    public string? TargetId { get; set; }

    /// <summary>Free-form human-readable context (e.g. "Scheduled -> Ongoing").</summary>
    public string? Detail { get; set; }

    // Timestamp is EntityBase.DateCreated (UTC).
}
