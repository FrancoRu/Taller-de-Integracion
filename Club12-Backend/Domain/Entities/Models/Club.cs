using System;
using System.Collections.Generic;

namespace Domain.Entities.Models;

/// <summary>
/// A club is the stable identity that persists across seasons, unlike a Team, which is a per-season registration record.
/// </summary>
public class Club : EntityBase
{
    /// <summary>
    /// The club's stable display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public club links, generated once from the name and never changed afterward.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Optional club crest or logo URL, independent of any season team logo.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Every per-season Team that belongs to this club, across every tournament.
    /// </summary>
    public virtual ICollection<Team> Teams { get; set; } = [];

    /// <summary>
    /// Optional link to the parent institution this club is a squad of, independent of tournament category. Flat, one level deep — a club with its own ChildClubs cannot itself have a ParentClubId.
    /// </summary>
    public Guid? ParentClubId { get; set; }
    public Club? ParentClub { get; set; }

    /// <summary>
    /// Other clubs (squads) that link to this club as their parent institution.
    /// </summary>
    public virtual ICollection<Club> ChildClubs { get; set; } = [];
}
