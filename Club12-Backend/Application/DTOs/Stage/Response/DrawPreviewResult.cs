using System.Collections.Generic;

namespace Application.DTOs.Stage.Response;

/// <summary>
/// The pairing a draw preview computed together with the signed token that replays it exactly on commit.
/// </summary>
public class DrawPreviewResult
{
    public required List<DrawPairPreview> Pairs { get; set; }

    public required string DrawToken { get; set; }
}
