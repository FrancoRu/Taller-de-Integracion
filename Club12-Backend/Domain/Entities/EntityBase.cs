using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities;

/// <summary>
/// Represents the base class for all entities.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// The unique identifier of the entity.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    /// <summary>
    /// The date when the entity was created.
    /// </summary>
    [Required]
    [Column("DateCreated")]
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date when the entity was last updated.
    /// </summary>
    [Column("DateUpdated")]
    public DateTime? DateUpdated { get; set; }
}
