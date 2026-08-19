import { GUID } from '@/modules/core/types/types';
import { UserFilterRequest } from '@/modules/user/type/user';

export const userKeys = {
  list: (filter?: UserFilterRequest) =>
    filter === undefined
      ? (['user', 'list'] as const)
      : (['user', 'list', filter] as const),
  byId: (id: GUID) => ['user', 'byId', id] as const,
};
