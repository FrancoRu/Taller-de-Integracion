import { GUID } from '@/modules/core/types/types';
import { IPlayerSanctionFiltered } from '@/modules/playerSanction/type/playerSanction.d';

export const playerSanctionKeys = {
  list: (filter?: IPlayerSanctionFiltered) =>
    filter === undefined
      ? (['playerSanction', 'list'] as const)
      : (['playerSanction', 'list', filter] as const),
  byId: (id: GUID) => ['playerSanction', 'byId', id] as const,
};
