import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IStageResponse, StageType } from '@/modules/stage/type/stage.d';
import { BracketEdge, BracketModel, BracketRound } from '@/modules/playoff/type/bracket.d';

/**
 * Canonical bracket depth order for the "main path" stage types. ThirdPlace
 * is intentionally excluded — it is rendered as a side slot, never as a
 * column in the main advancement line.
 */
const ROUND_ORDER: Partial<Record<StageType, number>> = {
  [StageType.QuarterFinal]: 1,
  [StageType.SemiFinal]: 2,
  [StageType.Final]: 3,
};

const isMainRoundStage = (stage: IStageResponse): boolean =>
  stage.stageType in ROUND_ORDER;

const sortMainStages = (stages: IStageResponse[]): IStageResponse[] =>
  [...stages].sort((a, b) => {
    const orderA = ROUND_ORDER[a.stageType] ?? Number.MAX_SAFE_INTEGER;
    const orderB = ROUND_ORDER[b.stageType] ?? Number.MAX_SAFE_INTEGER;
    if (orderA !== orderB) return orderA - orderB;
    return a.order - b.order;
  });

const buildRound = (stage: IStageResponse, matches: IMatchResponse[]): BracketRound => ({
  stageId: stage.id,
  stageType: stage.stageType,
  matches: matches.filter(match => match.stageId === stage.id),
});

const participantIds = (match: IMatchResponse): GUID[] =>
  [match.homeTeam?.id, match.visitorTeam?.id].filter((id): id is GUID => Boolean(id));

/**
 * Infers connectors from a source round into the next round by matching
 * winningTeamId to a participant of a match in the next round. Emits an
 * edge ONLY when exactly one next-round match matches the winner — see
 * "Ambiguity handling" in design.md.
 */
const buildEdgesForRoundPair = (
  sourceRound: BracketRound,
  nextRound: BracketRound
): BracketEdge[] => {
  const edges: BracketEdge[] = [];

  if (nextRound.matches.length === 0) return edges;

  for (const sourceMatch of sourceRound.matches) {
    const winnerId = sourceMatch.winningTeamId;
    if (!winnerId) continue;

    const targets = nextRound.matches.filter(nextMatch =>
      participantIds(nextMatch).includes(winnerId)
    );

    if (targets.length !== 1) continue;

    edges.push({ fromMatchId: sourceMatch.id, toMatchId: targets[0].id });
  }

  return edges;
};

export function buildBracket(
  stages: IStageResponse[],
  matches: IMatchResponse[]
): BracketModel {
  const eliminationStages = stages.filter(stage => stage.isElimination);

  const mainStages = sortMainStages(eliminationStages.filter(isMainRoundStage));
  const thirdPlaceStage = eliminationStages.find(
    stage => stage.stageType === StageType.ThirdPlace
  );

  const rounds = mainStages.map(stage => buildRound(stage, matches));
  const thirdPlace = thirdPlaceStage
    ? buildRound(thirdPlaceStage, matches)
    : undefined;

  const edges: BracketEdge[] = rounds
    .slice(0, -1)
    .flatMap((round, index) => buildEdgesForRoundPair(round, rounds[index + 1]));

  return { rounds, thirdPlace, edges };
}
