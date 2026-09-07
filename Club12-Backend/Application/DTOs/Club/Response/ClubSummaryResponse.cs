using System;

namespace Application.DTOs.Club.Response;

/// <summary>
/// A minimal club identity, used for pickers and for referencing a related club (parent/child) without pulling in its full season history.
/// </summary>
public class ClubSummaryResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }
}
