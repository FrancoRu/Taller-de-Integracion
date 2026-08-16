using Domain.Enums;

using System;

namespace Domain.Entities.Models;

public class PlayerSanction : EntityBase
{
    public required int Duration { get; set; }
    public required DateTime IssuedDate { get; set; }
    public required string Description { get; set; }
    public required Player Player { get; set; }
    public Guid PlayerId { get; set; }
    public required Match Match { get; set; }
    public Guid MatchId { get; set; }

    public SanctionAppealStatus AppealStatus { get; set; } = SanctionAppealStatus.None;
    public string? AppealReason { get; set; }
    public DateTime? AppealDate { get; set; }
    public string? AppealResolution { get; set; }
    public DateTime? AppealResolvedDate { get; set; }
}