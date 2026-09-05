using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// Represents a season in the Club12 application, a top-level grouping that gathers several tournaments played in the same competitive period.
/// </summary>
public class Season : EntityBase
{
    /// <summary>
    /// The display name of the season, in the form "Temporada XXVII".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public season links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The optional calendar year the season is played in.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// The tournaments grouped under this season, empty until tournaments are assigned since membership is optional.
    /// </summary>
    public virtual ICollection<Tournament> Tournaments { get; set; } = [];
}
