import { MatchFiltered } from '@/modules/match/type/match';

export const matchKeys = {
  list: (filter?: MatchFiltered) =>
    filter === undefined
      ? (['match', 'list'] as const)
      : (['match', 'list', filter] as const),
  byId: (id: string) => ['match', 'byId', id] as const,
  byRound: (stageId: string) => ['match', 'byRound', stageId] as const,
};
