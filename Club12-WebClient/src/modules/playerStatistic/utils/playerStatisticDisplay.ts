import { StatisticType } from '@/modules/playerStatistic/type/playerStatistic.d';

/**
 * Canonical Spanish display labels for every statistic type. Co-located here
 * so every view (player detail, team detail, create-statistic form) shows
 * the same wording instead of each screen inventing its own.
 */
export const STATISTIC_TYPE_LABELS: Record<StatisticType, string> = {
  Points: 'Puntos',
  Assists: 'Asistencias',
};
