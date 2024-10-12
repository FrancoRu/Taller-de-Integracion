using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.SancitonEntity;
/// <summary>
/// Represents a sancion in the Club12 application.
/// </summary>
[Table("Sanctions", Schema = "Club12")]
public class Sanction : EntityBase
{
    /// <summary>
    /// The name of the sanction     
    /// </summary>
    [Required]
    public required string Name { get; set; }

    /// <summary>
    /// The description of the sanction     
    /// </summary>
    [Required]
    public required string Description { get; set; }
}
