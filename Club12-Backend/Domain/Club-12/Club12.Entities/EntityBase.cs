using Club12.Entities.UserEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities;

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

    /// <summary>
    /// The user who created the entity.
    /// </summary>
    [Column("UserCreatedId")]
    public User? UserCreated { get; set; }

    /// <summary>
    /// The ID of the user who created the entity.
    /// </summary>
    public Guid? UserCreatedId { get; set; }

    /// <summary>
    /// The user who last updated the entity.
    /// </summary>
    [Column("UserUpdatedId")]
    public User? UserUpdated { get; set; }

    /// <summary>
    /// The ID of the user who last updated the entity.
    /// </summary>
    public Guid? UserUpdatedId { get; set; }
}
