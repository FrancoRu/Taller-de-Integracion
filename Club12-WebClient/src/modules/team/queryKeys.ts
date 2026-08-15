import { GUID } from '@/modules/core/types/types';
import { TeamFiltered } from '@/modules/team/type/team.d';

export const teamKeys = {
  list: (filter?: TeamFiltered) =>
    filter === undefined
      ? (['team', 'list'] as const)
      : (['team', 'list', filter] as const),
  byId: (id: GUID) => ['team', 'byId', id] as const,
};
