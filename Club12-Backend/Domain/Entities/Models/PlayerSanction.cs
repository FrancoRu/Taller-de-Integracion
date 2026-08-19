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

    /// <summary>
    /// The unique, URL-friendly identifier used in public/admin sanction
    /// links. Generated once from the sanctioned player's name and the
    /// sanction's issued date at creation time and never changed afterward,
    /// so shared links keep working even if the player is renamed.
    /// </summary>
    public required string Slug { get; set; }

    public SanctionAppealStatus AppealStatus { get; set; } = SanctionAppealStatus.None;
    public string? AppealReason { get; set; }
    public DateTime? AppealDate { get; set; }
    public string? AppealResolution { get; set; }
    public DateTime? AppealResolvedDate { get; set; }
}