import { GUID } from '@/modules/core/types/types';
import { IScorerFiltered, ScorerScope } from '@/modules/scorer/type/scorer.d';

/**
 * Fields a HU-85 scope selection contributes to the goleadores query. The
 * remaining `IScorerFiltered` fields (pagination, etc.) are merged in by the
 * caller.
 */
export type ScorerScopeParams = Pick<
  IScorerFiltered,
  'tournamentId' | 'divisionId' | 'stageId' | 'season'
>;

export interface ScorerScopeInput {
  tournamentId?: GUID | '';
  divisionId?: GUID | '';
  stageId?: GUID | '';
  season?: number | '';
}

/**
 * Maps the active HU-85 scope (per tournament / per season / all-time) to the
 * exact `Scorer/by-player` query params, so that:
 * - `tournament` sends `tournamentId` (plus the optional division/stage refinement),
 * - `season` sends `Season` only,
 * - `allTime` sends neither, yielding the all-time ranking.
 *
 * Empty-string form values are normalised to `undefined` so they are dropped
 * from the request instead of being sent as blanks.
 */
export const buildScorerScopeParams = (
  scope: ScorerScope,
  input: ScorerScopeInput
): ScorerScopeParams => {
  switch (scope) {
    case 'season': {
      const season =
        input.season === '' || input.season == null
          ? undefined
          : Number(input.season);
      return { season };
    }
    case 'allTime':
      return {};
    case 'tournament':
    default:
      return {
        tournamentId: input.tournamentId || undefined,
        divisionId: input.divisionId || undefined,
        stageId: input.stageId || undefined,
      };
  }
};
