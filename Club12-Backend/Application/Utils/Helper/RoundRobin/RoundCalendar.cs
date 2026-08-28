using System;

namespace Application.Utils.Helper.RoundRobin;

/// <summary>
/// Maps a 1-based round (jornada) number to its default calendar date
/// (HU-65): every round is played on a Sunday, one round per week, starting
/// from the first Sunday on or after the stage's start date. The round number
/// stays the canonical grouping key (HU-63) — this date is only a default the
/// admin can later move per match (HU-67/HU-68) without changing the round.
/// </summary>
public static class RoundCalendar
{
    /// <summary>
    /// The default calendar date for a round: the first Sunday on or after
    /// <paramref name="startDate"/>, advanced by one week per round.
    /// </summary>
    /// <param name="startDate">The stage's start date.</param>
    /// <param name="round">The 1-based round number.</param>
    public static DateTime SundayForRound(DateTime startDate, int round)
    {
        if (round < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(round), round, "Round numbers are 1-based.");
        }

        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)startDate.DayOfWeek + 7) % 7;
        DateTime firstSunday = startDate.Date.AddDays(daysUntilSunday);

        return firstSunday.AddDays(7 * (round - 1));
    }
}
