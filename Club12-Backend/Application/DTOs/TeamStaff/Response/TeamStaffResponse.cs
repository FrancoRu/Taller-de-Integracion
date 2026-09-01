using System;

namespace Application.DTOs.TeamStaff.Response;

/// <summary>
/// A team's technical staff (cuerpo técnico) member record, as returned by
/// the create and list endpoints.
/// </summary>
public class TeamStaffResponse
{
    /// <summary>The unique identifier of the staff member.</summary>
    public Guid Id { get; set; }

    /// <summary>The id of the team this staff member belongs to.</summary>
    public Guid TeamId { get; set; }

    /// <summary>The team's name, when the team was loaded.</summary>
    public string? TeamName { get; set; }

    /// <summary>The tournament (season) this staff membership belongs to.</summary>
    public Guid TournamentId { get; set; }

    /// <summary>The staff member's full name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>The role this person holds on the team's technical staff (e.g. "Coach").</summary>
    public required string Role { get; set; }

    /// <summary>When the staff member was added.</summary>
    public DateTime DateCreated { get; set; }
}
