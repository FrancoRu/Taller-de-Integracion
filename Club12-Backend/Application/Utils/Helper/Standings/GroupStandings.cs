using Domain.Entities.Models;

using System;
using System.Collections.Generic;

namespace Application.Utils.Helper.Standings;

/// <summary>
/// Standings for a single Group stage within a division.
/// </summary>
public class GroupStandings
{
    /// <summary>
    /// The id of the Group stage these standings belong to.
    /// </summary>
    public required Guid StageId { get; set; }

    /// <summary>
    /// The Group stage's name, used as the table label.
    /// </summary>
    public required string StageName { get; set; }

    /// <summary>
    /// The ordered standings for the teams in this group.
    /// </summary>
    public required List<Position> Positions { get; set; }
}
