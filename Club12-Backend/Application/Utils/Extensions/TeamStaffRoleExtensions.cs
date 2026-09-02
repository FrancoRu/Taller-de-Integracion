using Domain.Enums;

using System;

namespace Application.Utils.Extensions;

/// <summary>
/// Presentation helpers for <see cref="TeamStaffRole"/>. The enum itself stays
/// in English (a stable code identifier persisted by name), but any
/// human-readable text shown to users must be Spanish, so this maps each role
/// to its Spanish label.
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
