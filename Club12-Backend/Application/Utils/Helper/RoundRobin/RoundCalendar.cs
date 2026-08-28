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
    /// Weekday shift (in days) applied to a cross-division-cup group stage's
    /// jornadas relative to the regular-zone Sunday baseline (HU-111). Sunday +
    /// 3 = Wednesday, so a team that plays its zone on Sundays plays the cross
    /// cup on Wednesdays and the two jornadas can never fall on the same day.
    /// Chosen as a fixed, deterministic offset (not derived from any date) so
    /// the anti-collision guarantee holds on every jornada, every week.
    /// </summary>
    public const int CrossDivisionCupDayOffset = 3;

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

    /// <summary>
    /// Division-aware default date for a jornada (HU-111). Regular zones keep the
    /// Sunday baseline (<see cref="SundayForRound"/>); a cross-division-cup stage
    /// is shifted by <see cref="CrossDivisionCupDayOffset"/> so its weekly
    /// jornadas land on a different weekday and never collide with the zones a
    /// shared team also plays in. The round number stays the canonical grouping
    /// key — only the calendar date is division-aware.
    /// </summary>
    /// <param name="startDate">The stage's start date (the anchor).</param>
    /// <param name="round">The 1-based round number.</param>
    /// <param name="isCrossDivisionCup">Whether the stage's division is a cross-division cup.</param>
    public static DateTime DateForRound(DateTime startDate, int round, bool isCrossDivisionCup)
    {
        DateTime sunday = SundayForRound(startDate, round);
        return isCrossDivisionCup ? sunday.AddDays(CrossDivisionCupDayOffset) : sunday;
    }
}
