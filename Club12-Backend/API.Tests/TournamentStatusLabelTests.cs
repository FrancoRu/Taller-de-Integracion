using Application.Utils.Extensions;

using Domain.Enums;

namespace API.Tests;

/// <summary>
/// Pins the Spanish, user-facing labels for every TournamentStatus. The enum
/// stays in English (a stable, persisted code identifier) but any text shown to
/// users — the audit-trail Detail column among them — must be Spanish.
/// </summary>
public class TournamentStatusLabelTests
{
    [Theory]
    [InlineData(TournamentStatus.Scheduled, "Programado")]
    [InlineData(TournamentStatus.OpenForRegistration, "Inscripción abierta")]
    [InlineData(TournamentStatus.RegistrationClosed, "Inscripción cerrada")]
    [InlineData(TournamentStatus.Ongoing, "En curso")]
    [InlineData(TournamentStatus.Finished, "Finalizado")]
    [InlineData(TournamentStatus.Canceled, "Cancelado")]
    public void ToSpanishLabel_MapsEveryStatus_ToItsSpanishLabel(
        TournamentStatus status, string expected)
    {
        Assert.Equal(expected, status.ToSpanishLabel());
    }
}
