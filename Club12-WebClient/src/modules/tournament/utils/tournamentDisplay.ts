import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { formatDateAr } from '@/modules/core/utils/formatDate';

export const TOURNAMENT_STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programado',
  OpenForRegistration: 'Inscripción abierta',
  RegistrationClosed: 'Inscripción cerrada',
  Ongoing: 'En curso',
  Finished: 'Finalizado',
  Canceled: 'Cancelado',
};

export const TOURNAMENT_STATUS_COLOR: Record<
  TournamentStatus,
  'default' | 'info' | 'secondary' | 'warning' | 'success' | 'error'
> = {
  Scheduled: 'default',
  OpenForRegistration: 'info',
  RegistrationClosed: 'secondary',
  Ongoing: 'warning',
  Finished: 'success',
  Canceled: 'error',
};

/**
 * Narrows an unknown status value (as received from the API) to a known
 * TournamentStatus, defaulting to Scheduled for anything unrecognized. Shared
 * so every list/detail view resolves status the same way and no view silently
 * coerces a valid status it forgot to whitelist.
 */
export const resolveTournamentStatus = (status: unknown): TournamentStatus => {
  if (
    typeof status === 'string' &&
    (Object.values(TournamentStatus) as string[]).includes(status)
  ) {
    return status as TournamentStatus;
  }

  return TournamentStatus.Scheduled;
};

export const formatTournamentDate = (value: Date | string): string =>
  formatDateAr(value);
