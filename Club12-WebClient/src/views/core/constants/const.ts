import { GUID } from '@/modules/core/types/types';

export const GUID_EMPTY: GUID = '00000000-0000-0000-0000-000000000000';

export const MIN_TEAM: number = 4;
export const MAX_TEAM: number = 24;
export enum MatchSide {
  HOME = 'home',
  VISITOR = 'visitor',
}
