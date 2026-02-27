using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Models;

/// <summary>
/// Represents the association between a stage and a team within a match context.
/// </summary>
/// <remarks>This class is typically used to model the participation of a team in a specific stage of a
/// competition or tournament. It links a team to a stage, enabling queries and operations that involve both
/// entities.</remarks>
[Table("StageTeamMatches", Schema = "Club12")]
public class StageTeamMatch: EntityBase
{
    /// <summary>
    /// The unique identifier of the stage that this team is associated with in the context of a match.
    /// </summary>
    public required Guid StageId { get; set; }

    /// <summary>
    /// The navigation property to the related stage entity. This allows access to the details of the stage
    /// </summary>
    [ForeignKey(nameof(StageId))]
    public Stage? Stage { get; set; }

    /// <summary>
    /// The unique identifier of the team that is participating in the stage within the context of a match.
    /// </summary>
    public required Guid TeamId { get; set; }

    /// <summary>
    /// The navigation property to the related team entity. This allows access to the details of the team that
    /// </summary>
    [ForeignKey(nameof(TeamId))]
    public Team? Team { get; set; }
}
