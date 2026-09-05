using System.Collections.Generic;
using System.Linq;

namespace Domain.Enums;

/// <summary>
/// Encodes the tournament lifecycle state machine. The happy path is linear
/// — Scheduled -> OpenForRegistration -> RegistrationClosed -> Ongoing ->
/// Finished — with <see cref="TournamentStatus.Canceled"/> reachable from any
/// non-terminal state. The one exception to "forward-only" is
/// Ongoing -> RegistrationClosed (see the inline comment below), a deliberate
/// "revertir a borrador" escape hatch. <see cref="TournamentStatus.Finished"/>
/// and <see cref="TournamentStatus.Canceled"/> are terminal: no transition
/// leaves them.
/// </summary>
public static class TournamentStatusTransitions
{
    /// <summary>
    /// The set of states each status may move to next. A status absent from a
    /// value list is an invalid (backward, skipping, or out-of-terminal)
    /// transition.
    /// </summary>
    private static readonly IReadOnlyDictionary<TournamentStatus, TournamentStatus[]> AllowedNextStates =
        new Dictionary<TournamentStatus, TournamentStatus[]>
        {
            [TournamentStatus.Scheduled] = [TournamentStatus.OpenForRegistration, TournamentStatus.Canceled],
            [TournamentStatus.OpenForRegistration] = [TournamentStatus.RegistrationClosed, TournamentStatus.Canceled],
            [TournamentStatus.RegistrationClosed] = [TournamentStatus.Ongoing, TournamentStatus.Canceled],
            // Ongoing may be reverted back to RegistrationClosed ("revertir a
            // borrador"): this reopens division assignment so a mis-assignment
            // can be fixed, and tears down the generated fixture so re-starting
            // rebuilds it from the corrected assignment.
            [TournamentStatus.Ongoing] = [TournamentStatus.Finished, TournamentStatus.RegistrationClosed, TournamentStatus.Canceled],
            [TournamentStatus.Finished] = [],
            [TournamentStatus.Canceled] = [],
        };

    /// <summary>
    /// Whether a status is terminal (no outgoing transition).
    /// </summary>
    public static bool IsTerminal(TournamentStatus status) =>
        status is TournamentStatus.Finished or TournamentStatus.Canceled;

    /// <summary>
    /// Whether moving from <paramref name="from"/> to <paramref name="to"/> is
    /// allowed. A no-op transition to the same status is always considered
    /// valid (callers treat it as a no-op); every other move must appear in
    /// <see cref="AllowedNextStates"/>.
    /// </summary>
    public static bool IsValidTransition(TournamentStatus from, TournamentStatus to)
    {
        if (from == to)
        {
            return true;
        }

        return AllowedNextStates.TryGetValue(from, out TournamentStatus[]? nextStates)
            && nextStates.Contains(to);
    }
}
