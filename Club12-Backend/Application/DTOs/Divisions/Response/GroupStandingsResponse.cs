using System;
using System.Collections.Generic;

namespace Application.DTOs.Divisions.Response;

/// <summary>
/// The standings of a single Group stage; a multi-group division sends one entry per internal group.
/// </summary>
public class GroupStandingsResponse
{
    /// <summary>
    /// The id of the Group stage these standings belong to.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// The Group stage's name, used as the table label.
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// The ordered standings for the teams in this group.
    /// </summary>
    public List<PositionResponse> Positions { get; set; } = [];
}
