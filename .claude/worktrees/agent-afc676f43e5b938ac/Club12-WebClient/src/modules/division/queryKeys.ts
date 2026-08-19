import { GUID } from '@/modules/core/types/types';
import { DivisionFiltered } from '@/modules/division/type/division';

export const divisionKeys = {
  list: (filter?: DivisionFiltered) =>
    filter === undefined
      ? (['division', 'list'] as const)
      : (['division', 'list', filter] as const),
  byId: (id: GUID) => ['division', 'byId', id] as const,
};
