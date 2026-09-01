using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TeamStaff.Request;

/// <summary>
/// Request to add a member to a team's technical staff (cuerpo técnico). The
/// team is taken from the route; the tournament (season) scopes the
/// membership, mirroring the Team+Tournament scoping used by
/// PlayerTeamRegistration.
/// </summary>
public class CreateTeamStaffRequest
{
    /// <summary>
    /// The staff member's full name.
    /// </summary>
    [Required(ErrorMessage = "The FullName field is required.")]
    [MaxLength(150, ErrorMessage = "FullName cannot exceed 150 characters.")]
    public required string FullName { get; set; }

    /// <summary>
    /// The role this person holds on the team's technical staff.
    /// </summary>
    public required TeamStaffRole Role { get; set; }

    /// <summary>
    /// The tournament (season) this staff membership belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }
}
