using System;

namespace Application.DTOs.Divisions.Response;

/// <summary>
/// One position-range → playoff-destination entry of a division (HU-45).
/// </summary>
public class PlayoffMappingResponse
{
    public Guid Id { get; set; }
    public int FromPosition { get; set; }
    public int ToPosition { get; set; }
    public string Destination { get; set; } = string.Empty;
}
