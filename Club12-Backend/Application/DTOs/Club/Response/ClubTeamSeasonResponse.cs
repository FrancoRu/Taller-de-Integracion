using System;
using System.Collections.Generic;

namespace Application.DTOs.Club.Response;

/// <summary>
/// One per-season team row belonging to a club, with the seasons it participated in.
/// </summary>
public class ClubTeamSeasonResponse
{
    public required Guid TeamId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The tournaments this team was registered in, sourced from its registration records.
    /// </summary>
    public required List<ClubSeasonResponse> Seasons { get; set; } = [];
}
