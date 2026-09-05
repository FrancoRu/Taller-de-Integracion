using Domain.Constants;

using System;

namespace Domain.Entities.Models;

/// <summary>
/// Base class for all domain entities, centralizing the identifier and the created and updated audit fields shared by every entity.
/// </summary>
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.Empty;

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Null until the entity is modified after creation.
    /// </summary>
    public DateTime? DateUpdated { get; set; }

    public required string CreatedBy { get; set; } = AuditConstants.SystemUser;
    public string? UpdatedBy { get; set; }
}
