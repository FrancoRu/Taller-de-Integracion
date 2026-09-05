using Domain.Enums;

using System;

namespace Application.Utils.Extensions;

/// <summary>
/// Presentation helpers for TournamentStatus, mapping each status to its Spanish label for user-facing text.
/// </summary>
public static class TournamentStatusExtensions
{
    /// <summary>
    /// Returns the Spanish, user-facing label for a tournament status.
    /// </summary>
    public static string ToSpanishLabel(this TournamentStatus status)
    {
        return status switch
        {
            TournamentStatus.Scheduled => "Programado",
            TournamentStatus.OpenForRegistration => "Inscripción abierta",
            TournamentStatus.RegistrationClosed => "Inscripción cerrada",
            TournamentStatus.Ongoing => "En curso",
            TournamentStatus.Finished => "Finalizado",
            TournamentStatus.Canceled => "Cancelado",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }
}
