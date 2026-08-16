using Domain.Constants;

using System;

namespace Domain.Entities.Models;

/// <summary>Base class for all domain entities.</summary>
public abstract class EntityBase
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.Empty;

    /// <summary>
    /// The date when the entity was created.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date when the entity was last updated.
    /// </summary>
    public DateTime? DateUpdated { get; set; }

    public required string CreatedBy { get; set; } = AuditConstants.SystemUser;
    public string? UpdatedBy { get; set; }
}
