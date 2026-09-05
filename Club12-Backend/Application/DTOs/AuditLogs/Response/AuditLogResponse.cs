using Application.DTOs.Abstract.Response;

using System;

namespace Application.DTOs.AuditLogs.Response;

/// <summary>
/// Audit-trail entry as returned by the listing endpoint.
/// </summary>
public class AuditLogResponse : BaseEntityResponse
{
    /// <summary>
    /// The sensitive action that was performed, as the enum name.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Who performed the action, as an email address or "System".
    /// </summary>
    public required string Actor { get; set; }

    /// <summary>
    /// The kind of entity targeted, when applicable.
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// Identifier of the targeted entity, when applicable.
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// Null when there is no single named target or the entry predates this field.
    /// </summary>
    public string? TargetName { get; set; }

    /// <summary>
    /// Free-form human-readable context.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// When the action happened, in UTC.
    /// </summary>
    public required DateTime Timestamp { get; set; }
}
