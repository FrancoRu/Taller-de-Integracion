using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.TeamStaff.Request;

/// <summary>
/// Request to add a member to a team's technical staff; the team is taken from the route.
/// </summary>
public class CreateTeamStaffRequest
{
    [Required(ErrorMessage = "The FullName field is required.")]
    [MaxLength(150, ErrorMessage = "FullName cannot exceed 150 characters.")]
    public required string FullName { get; set; }

    public required TeamStaffRole Role { get; set; }

    /// <summary>
    /// The tournament this staff membership belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }
}
