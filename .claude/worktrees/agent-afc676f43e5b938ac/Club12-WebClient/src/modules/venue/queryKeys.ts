import { GUID } from '@/modules/core/types/types';

export const venueKeys = {
  list: () => ['venue', 'list'] as const,
  byId: (id: GUID) => ['venue', 'byId', id] as const,
};
