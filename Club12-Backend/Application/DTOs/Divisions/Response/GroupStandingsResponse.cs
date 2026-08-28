using System;
using System.Collections.Generic;

namespace Application.DTOs.Divisions.Response;

/// <summary>
/// The standings of a single Group stage within a division. A division with
/// more than one Group stage (a multi-group cross-division cup, HU-110) sends
/// one entry per internal group so the public tournament view can render one
/// POSICIONES table per group, each labelled by <see cref="StageName"/>.
/// </summary>
public class GroupStandingsResponse
{
    /// <summary>The id of the Group stage these standings belong to.</summary>
    public Guid StageId { get; set; }

    /// <summary>The Group stage's name, used as the table label (e.g. "Grupo 1").</summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>The ordered standings for the teams in this group.</summary>
    public List<PositionResponse> Positions { get; set; } = [];
}
