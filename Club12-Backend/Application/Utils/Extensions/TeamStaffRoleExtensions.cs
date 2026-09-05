using Domain.Enums;

using System;

namespace Application.Utils.Extensions;

/// <summary>
/// Presentation helpers for TeamStaffRole, mapping each role to its Spanish label for user-facing text.
/// </summary>
public static class TeamStaffRoleExtensions
{
    /// <summary>
    /// Returns the Spanish, user-facing label for a team-staff role.
    /// </summary>
    public static string ToSpanishLabel(this TeamStaffRole role)
    {
        return role switch
        {
            TeamStaffRole.Coach => "DT",
            TeamStaffRole.AssistantCoach => "Asistente",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
        };
    }
}
