using System;

namespace Application.Utils.Helper.RoundRobin;

/// <summary>
/// Maps a 1-based round, jornada, number to its default calendar date.
/// </summary>
public static class RoundCalendar
{
    /// <summary>
    /// Weekday shift, in days, applied to a cross-division-cup group stage's jornadas relative to the regular-zone Sunday baseline.
    /// </summary>
    public const int CrossDivisionCupDayOffset = 3;

    /// <summary>
    /// The default calendar date for a round: the first Sunday on or after startDate, advanced by one week per round.
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

    /// <summary>
    /// Division-aware default date for a jornada.
    /// </summary>
    /// <param name="startDate">The stage's start date, the anchor.</param>
    /// <param name="round">The 1-based round number.</param>
    /// <param name="isCrossDivisionCup">Whether the stage's division is a cross-division cup.</param>
    public static DateTime DateForRound(DateTime startDate, int round, bool isCrossDivisionCup)
    {
        DateTime sunday = SundayForRound(startDate, round);
        return isCrossDivisionCup ? sunday.AddDays(CrossDivisionCupDayOffset) : sunday;
    }
}
