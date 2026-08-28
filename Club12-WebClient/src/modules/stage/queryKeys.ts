import { StageFiltered } from '@/modules/stage/type/stage';

export const stageKeys = {
  list: (filter?: StageFiltered) =>
    filter === undefined
      ? (['stage', 'list'] as const)
      : (['stage', 'list', filter] as const),
  byId: (idOrSlug: string) => ['stage', 'byId', idOrSlug] as const,
};
