using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a season ("Temporada") in the Club12 application: a top-level
/// grouping that gathers several tournaments played in the same competitive
/// period (for example a masculine and a feminine tournament). A tournament
/// keeps its own category; belonging to a season is optional and purely
/// additive — it never changes how a tournament defines its category (HU-48).
/// </summary>
public class Season : EntityBase
{
    /// <summary>
    /// The display name of the season, e.g. "Temporada XXVII".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public season links.
    /// Generated once from the name at creation time and never changed
    /// afterward, so shared links keep working even if the season is renamed.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The calendar year the season is played in (optional).
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The tournaments grouped under this season. Empty until tournaments are
    /// assigned; a tournament's membership is optional (a tournament may belong
    /// to no season at all).
    /// </summary>
    public virtual ICollection<Tournament> Tournaments { get; set; } = [];
}
