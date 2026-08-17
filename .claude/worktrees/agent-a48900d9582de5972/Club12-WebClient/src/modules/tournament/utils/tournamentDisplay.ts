import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';

export const TOURNAMENT_STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programado',
  OpenForRegistration: 'Inscripción abierta',
  Ongoing: 'En curso',
  Finished: 'Finalizado',
  Canceled: 'Cancelado',
};

export const TOURNAMENT_STATUS_COLOR: Record<TournamentStatus, 'default' | 'info' | 'warning' | 'success' | 'error'> = {
  Scheduled: 'default',
  OpenForRegistration: 'info',
  Ongoing: 'warning',
  Finished: 'success',
  Canceled: 'error',
};

export const formatTournamentDate = (value: Date | string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString('es-AR');
};
