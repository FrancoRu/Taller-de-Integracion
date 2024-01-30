using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.DivisionEntity;
/// <summary>
/// Represents a division in the Club12 application.
/// </summary>
[Table("Divisions", Schema = "Club12")]
public class Division : EntityBase
{
    /// <summary>
    /// The name of the Division     
    /// </summary>
    [Required]
    public required string Name { get; set; }
}
