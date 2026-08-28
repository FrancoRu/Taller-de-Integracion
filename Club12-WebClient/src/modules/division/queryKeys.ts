import { DivisionFiltered } from '@/modules/division/type/division';

export const divisionKeys = {
  list: (filter?: DivisionFiltered) =>
    filter === undefined
      ? (['division', 'list'] as const)
      : (['division', 'list', filter] as const),
  byId: (idOrSlug: string) => ['division', 'byId', idOrSlug] as const,
};
