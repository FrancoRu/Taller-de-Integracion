import { IPlayerSanctionFiltered } from '@/modules/playerSanction/type/playerSanction.d';

export const playerSanctionKeys = {
  list: (filter?: IPlayerSanctionFiltered) =>
    filter === undefined
      ? (['playerSanction', 'list'] as const)
      : (['playerSanction', 'list', filter] as const),
  byId: (idOrSlug: string) => ['playerSanction', 'byId', idOrSlug] as const,
};
