using System.Collections.Generic;
using System.Linq;

namespace Domain.Enums;

/// <summary>
/// Encodes the tournament lifecycle state machine, a linear happy path from Scheduled through Finished plus a Canceled escape from any non-terminal state.
/// </summary>
public static class TournamentStatusTransitions
{
    /// <summary>
    /// The set of states each status may move to next, an absent status meaning an invalid transition.
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
    /// Whether a status is terminal, meaning it has no outgoing transition.
    /// </summary>
    public static bool IsTerminal(TournamentStatus status) =>
        status is TournamentStatus.Finished or TournamentStatus.Canceled;

    /// <summary>
    /// Whether transitioning from one given status to another is allowed, with a same-status no-op always considered valid.
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
