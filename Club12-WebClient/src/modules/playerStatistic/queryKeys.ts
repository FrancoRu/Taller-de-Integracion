import { GUID } from '@/modules/core/types/types';

export const playerStatisticKeys = {
  all: ['playerStatistic'] as const,
  /** A player's statistic card (HU-87). */
  card: (playerId: GUID) => ['playerStatistic', 'card', playerId] as const,
  /** A player's cross-season history (HU-88). */
  history: (playerId: GUID) =>
    ['playerStatistic', 'history', playerId] as const,
};
