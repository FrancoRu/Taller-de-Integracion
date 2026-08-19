import { GUID } from '@/modules/core/types/types';
import { MatchFiltered } from '@/modules/match/type/match';

export const matchKeys = {
  list: (filter?: MatchFiltered) =>
    filter === undefined
      ? (['match', 'list'] as const)
      : (['match', 'list', filter] as const),
  byId: (id: GUID) => ['match', 'byId', id] as const,
};
