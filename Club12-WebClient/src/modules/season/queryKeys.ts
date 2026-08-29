import { SeasonFiltered } from '@/modules/season/type/season';

export const seasonKeys = {
  all: ['season'] as const,
  list: (filter?: SeasonFiltered) => ['season', 'list', filter ?? null] as const,
  byId: (idOrSlug: string) => ['season', 'byId', idOrSlug] as const,
};
