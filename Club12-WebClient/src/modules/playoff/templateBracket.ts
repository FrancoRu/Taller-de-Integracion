import { GUID } from '@/modules/core/types/types';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { StageType } from '@/modules/stage/type/stage';
import { BracketModel, BracketRound } from '@/modules/playoff/type/bracket.d';
import { CupConfig, qualifiersToStageTypes } from '@/views/tournament/wizard/types';

/**
 * How many match slots a bracket round has, counting backward from a
 * single-match Final — mirrors {@link qualifiersToStageTypes}'s own depth
 * buckets, since the two must never disagree about the bracket's shape.
 */
const MATCHES_IN_ROUND: Partial<Record<StageType, number>> = {
  [StageType.Final]: 1,
  [StageType.SemiFinal]: 2,
  [StageType.QuarterFinal]: 4,
  [StageType.RoundOf16]: 8,
};

/**
 * An empty, unplayed placeholder match for a bracket preview — every field
 * that would identify real participants or a result is null/false, so it
 * renders as a plain TBD slot through the existing bracket components
 * (the same shape `bracketAdapter.ts`'s own `placeholderRawMatch` uses for
 * an unfilled round in a real, partially-drawn bracket).
 */
const placeholderMatch = (id: GUID, stageId: GUID): IMatchResponse => ({
  id,
  matchDate: '',
  matchType: MatchType.Playoff,
  slug: '',
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  stageId,
});

/**
 * Builds a preview {@link BracketModel} for a cup that has not been drawn
 * yet (or has no persisted matches at all) — every round the cup's
 * qualifier count implies ({@link qualifiersToStageTypes}), populated with
 * empty placeholder slots instead of real matches. Reuses the exact same
 * rendering pipeline (`PlayoffBracket`, `toLibraryMatches`) a real bracket
 * uses: with no `edges` to infer a connector from, that pipeline already
 * falls back to positional pairing (round N's match `i` feeds round N+1's
 * match `floor(i/2)`), which is exactly a template bracket's shape.
 */
export const buildTemplateBracketModel = (cup: CupConfig): BracketModel => {
  const stageTypes = qualifiersToStageTypes(cup.qualifiers, cup.hasThirdPlace);
  const mainStageTypes = stageTypes.filter(stageType => stageType !== StageType.ThirdPlace);

  const rounds: BracketRound[] = mainStageTypes.map(stageType => {
    const stageId = `template-${cup.id}-${stageType}` as GUID;
    const matchCount = MATCHES_IN_ROUND[stageType] ?? 1;

    return {
      stageId,
      stageType,
      matches: Array.from({ length: matchCount }, (_, index) =>
        placeholderMatch(`${stageId}-${index}` as GUID, stageId)
      ),
    };
  });

  const hasThirdPlace = stageTypes.includes(StageType.ThirdPlace);
  const thirdPlaceStageId = `template-${cup.id}-${StageType.ThirdPlace}` as GUID;

  return {
    rounds,
    thirdPlace: hasThirdPlace
      ? {
          stageId: thirdPlaceStageId,
          stageType: StageType.ThirdPlace,
          matches: [placeholderMatch(`${thirdPlaceStageId}-0` as GUID, thirdPlaceStageId)],
        }
      : undefined,
    edges: [],
  };
};
