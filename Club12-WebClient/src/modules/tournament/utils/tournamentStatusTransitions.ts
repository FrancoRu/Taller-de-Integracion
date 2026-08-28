import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';

/**
 * Client mirror of the backend lifecycle state machine
 * (Domain.Enums.TournamentStatusTransitions). Transitions are forward-only
 * along Scheduled -> OpenForRegistration -> RegistrationClosed -> Ongoing ->
 * Finished, with Canceled reachable from any non-terminal state. Finished and
 * Canceled are terminal. The backend is the source of truth (an invalid
 * transition is rejected with 409); this map only keeps the UI from offering
 * moves the server would reject.
 */
export const TOURNAMENT_STATUS_NEXT_STATES: Record<
  TournamentStatus,
  TournamentStatus[]
> = {
  [TournamentStatus.Scheduled]: [
    TournamentStatus.OpenForRegistration,
    TournamentStatus.Canceled,
  ],
  [TournamentStatus.OpenForRegistration]: [
    TournamentStatus.RegistrationClosed,
    TournamentStatus.Canceled,
  ],
  [TournamentStatus.RegistrationClosed]: [
    TournamentStatus.Ongoing,
    TournamentStatus.Canceled,
  ],
  [TournamentStatus.Ongoing]: [
    TournamentStatus.Finished,
    TournamentStatus.Canceled,
  ],
  [TournamentStatus.Finished]: [],
  [TournamentStatus.Canceled]: [],
};

/**
 * The valid next statuses a tournament may transition to from its current
 * status (excluding the current status itself). Empty for terminal statuses.
 */
export const getNextStatusOptions = (
  current: TournamentStatus
): TournamentStatus[] => TOURNAMENT_STATUS_NEXT_STATES[current] ?? [];

/**
 * Whether moving from `from` to `to` is allowed. A no-op transition to the
 * same status is treated as valid (the backend ignores it).
 */
export const isValidStatusTransition = (
  from: TournamentStatus,
  to: TournamentStatus
): boolean => from === to || getNextStatusOptions(from).includes(to);

/** Whether a status has no outgoing transition (Finished or Canceled). */
export const isTerminalStatus = (status: TournamentStatus): boolean =>
  getNextStatusOptions(status).length === 0;
