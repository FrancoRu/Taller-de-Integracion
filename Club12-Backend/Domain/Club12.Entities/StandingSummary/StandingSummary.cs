using Club12.Entities.TeamEntity;
using Club12.Entities.TournamentEntity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club12.Entities.StandingSummaryEntity;

/// <summary>
/// Represents a standing summary in the Club12 application.
/// </summary>
[Table("StandingsSummaries", Schema = "Club12")]
public class StandingSummary : EntityBase
{
    /// <summary>
    /// The position of the team in the standings.
    /// </summary>
    [Required]
    public required int Position { get; set; }

    /// <summary>
    /// Represents the tournament associated with the standing summary.
    /// </summary>
    [Required]
    [Column("TournamentId")]
    public required Tournament Tournament { get; set; }

    /// <summary>
    /// Represents the ID of the tournament associated with the standing summary.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// Represents the team associated with the standing summary.
    /// </summary>
    [Required]
    [Column("TeamId")]
    public required Team Team { get; set; }

    /// <summary>
    /// Represents the ID of the team associated with the standing summary.
    /// </summary>
    public Guid TeamId { get; set; }
}
