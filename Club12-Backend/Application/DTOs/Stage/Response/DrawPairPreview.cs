using System;

namespace Application.DTOs.Stage.Response;

/// <summary>
/// One first-round bracket pairing in a draw preview, with a null visitor meaning the home team gets a bye.
/// </summary>
public class DrawPairPreview
{
    public required Guid HomeTeamId { get; set; }

    public Guid? VisitorTeamId { get; set; }
}
