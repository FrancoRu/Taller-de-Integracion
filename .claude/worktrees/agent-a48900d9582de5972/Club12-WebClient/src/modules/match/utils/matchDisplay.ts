export const MATCH_FINISHED_LABEL = 'Finalizado';
export const MATCH_SCHEDULED_LABEL = 'Programado';

export const MATCH_FINISHED_COLOR = 'success' as const;
export const MATCH_SCHEDULED_COLOR = 'default' as const;

export const getMatchStatusLabel = (finished: boolean): string =>
  finished ? MATCH_FINISHED_LABEL : MATCH_SCHEDULED_LABEL;

export const getMatchStatusColor = (finished: boolean) =>
  finished ? MATCH_FINISHED_COLOR : MATCH_SCHEDULED_COLOR;
