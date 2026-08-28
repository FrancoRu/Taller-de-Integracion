import { PlayerFiltered } from '@/modules/player/type/player.d';

export const playerKeys = {
  list: (filter?: PlayerFiltered) =>
    filter === undefined
      ? (['player', 'list'] as const)
      : (['player', 'list', filter] as const),
  byId: (idOrSlug: string, isAdministrative?: boolean) =>
    isAdministrative === undefined
      ? (['player', 'byId', idOrSlug] as const)
      : (['player', 'byId', idOrSlug, isAdministrative] as const),
};
