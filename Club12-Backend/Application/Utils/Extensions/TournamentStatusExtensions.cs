using Domain.Enums;

using System;

namespace Application.Utils.Extensions;

/// <summary>
/// Presentation helpers for <see cref="TournamentStatus"/>. The enum itself
/// stays in English (a stable code identifier persisted by name), but any
/// human-readable text shown to users — e.g. the audit-trail Detail column —
/// must be Spanish, so this maps each status to its Spanish label.
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
