import { IScorerByTeamFiltered, IScorerFiltered } from '@/modules/scorer/type/scorer.d';

export const scorerKeys = {
  byTeam: (filter: IScorerByTeamFiltered) =>
    ['scorer', 'byTeam', filter] as const,
  byPlayer: (filter: IScorerFiltered) =>
    ['scorer', 'byPlayer', filter] as const,
};
