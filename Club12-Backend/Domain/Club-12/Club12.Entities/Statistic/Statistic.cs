using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.StatisticEntity;
/// <summary>
/// Represents a Statistic in the Club12 application.
/// </summary>
[Table("Statistics", Schema = "Club12")]
public class Statistic : EntityBase
{
    /// <summary>
    /// The name of the statistic.     
    /// </summary>
    [Required]
    public required string Name { get; set; }
}
