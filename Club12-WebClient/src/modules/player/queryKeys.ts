import { GUID } from '@/modules/core/types/types';
import { PlayerFiltered } from '@/modules/player/type/player.d';

export const playerKeys = {
  list: (filter?: PlayerFiltered) =>
    filter === undefined
      ? (['player', 'list'] as const)
      : (['player', 'list', filter] as const),
  byId: (id: GUID, isAdministrative?: boolean) =>
    isAdministrative === undefined
      ? (['player', 'byId', id] as const)
      : (['player', 'byId', id, isAdministrative] as const),
};
