import { TeamFiltered } from '@/modules/team/type/team.d';

export const teamKeys = {
  list: (filter?: TeamFiltered) =>
    filter === undefined
      ? (['team', 'list'] as const)
      : (['team', 'list', filter] as const),
  byId: (id: string) => ['team', 'byId', id] as const,
};
