import { GUID } from '@/modules/core/types/types';
import { StageFiltered } from '@/modules/stage/type/stage.d';

export const stageKeys = {
  list: (filter?: StageFiltered) =>
    filter === undefined
      ? (['stage', 'list'] as const)
      : (['stage', 'list', filter] as const),
  byId: (id: GUID) => ['stage', 'byId', id] as const,
};
