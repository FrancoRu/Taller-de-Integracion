using System;
using System.Collections.Generic;

namespace Application.DTOs.Club.Response;

/// <summary>
/// A club's stable identity plus every per-season team that belongs to it.
/// </summary>
public class ClubHistoryResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>
    /// The per-season teams that make up this club's history.
    /// </summary>
    public required List<ClubTeamSeasonResponse> Teams { get; set; } = [];

    /// <summary>
    /// The parent institution this club is a squad of, or null when this club has no parent linked.
    /// </summary>
    public ClubSummaryResponse? ParentClub { get; set; }

    /// <summary>
    /// Other squads linked to this club as their parent institution, empty when this club has none.
    /// </summary>
    public required List<ClubSummaryResponse> ChildClubs { get; set; } = [];
}
