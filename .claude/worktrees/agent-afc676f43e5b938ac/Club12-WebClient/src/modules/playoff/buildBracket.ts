import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { BracketEdge, BracketGroup, BracketModel, BracketRound } from '@/modules/playoff/type/bracket.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';

/**
 * Canonical bracket depth order for the "main path" stage types. ThirdPlace
 * is intentionally excluded — it is rendered as a side slot, never as a
 * column in the main advancement line.
 */
const ROUND_ORDER: Partial<Record<StageType, number>> = {
  [StageType.RoundOf16]: 1,
  [StageType.QuarterFinal]: 2,
  [StageType.SemiFinal]: 3,
  [StageType.Final]: 4,
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

/**
 * Groups admin-defined free text used to key stages that carry no
 * BracketName into the division's single/default bracket.
 */
const DEFAULT_BRACKET_KEY = '__default__';

/**
 * Groups a division's stages by their (admin-defined, free-text)
 * BracketName. Stages with no BracketName share one default group, so a
 * division with a single elimination path still renders as one bracket.
 */
export function groupStagesByBracket(
  stages: IStageResponse[]
): Map<string, IStageResponse[]> {
  const groups = new Map<string, IStageResponse[]>();

  for (const stage of stages) {
    const key = stage.bracketName?.trim() || DEFAULT_BRACKET_KEY;
    const group = groups.get(key) ?? [];
    group.push(stage);
    groups.set(key, group);
  }

  return groups;
}

/**
 * Tallies how many games of a series each side has won so far, by
 * matching each finished game's winner name against the series'
 * home/visitor team names.
 */
export function countSeriesWins(series: IMatchSeriesResponse): {
  home: number;
  visitor: number;
} {
  let home = 0;
  let visitor = 0;

  for (const game of series.games) {
    if (!game.isFinished) continue;
    if (game.winningTeamName === series.homeTeamName) home += 1;
    else if (game.winningTeamName === series.visitorTeamName) visitor += 1;
  }

  return { home, visitor };
}

/**
 * Collapses a best-of-N series into a single bracket node: an
 * IMatchResponse-shaped object carrying the aggregate game-win tally as
 * each side's "score", so the existing bracket rendering (built for one
 * match per pairing) can display series without any changes. The series
 * is only shown as "finished" (enabling the win highlight) once it has
 * actually been decided — an in-progress 1-0 lead never falsely reads as
 * a completed round.
 */
export function seriesToRepresentativeMatch(series: IMatchSeriesResponse): IMatchResponse {
  const { home, visitor } = countSeriesWins(series);

  return {
    id: series.id,
    matchDate: series.games[0]?.matchDate ?? '',
    matchType: (series.games[0]?.matchType ?? 'Playoff') as IMatchResponse['matchType'],
    slug: '',
    homeTeam: {
      id: series.homeTeamId,
      name: series.homeTeamName,
      logoUrl: '',
      score: home,
      players: [],
      scorers: [],
    },
    visitorTeam: {
      id: series.visitorTeamId,
      name: series.visitorTeamName,
      logoUrl: '',
      score: visitor,
      players: [],
      scorers: [],
    },
    isFinished: home + visitor > 0,
    winningTeamId: series.winningTeamId,
    winningTeamName: series.winningTeamName,
    venue: null,
    stageId: series.stageId,
  };
}

/**
 * Builds one BracketModel per named bracket in a division (grouped by
 * BracketName — free text, admin-defined, optional). Stages with
 * BestOf > 1 are rendered as one node per series (using
 * `seriesByStageId`) rather than one node per individual game.
 */
export function buildBrackets(
  stages: IStageResponse[],
  matches: IMatchResponse[],
  seriesByStageId: Map<GUID, IMatchSeriesResponse[]> = new Map()
): BracketGroup[] {
  const stageGroups = groupStagesByBracket(stages);

  return [...stageGroups.entries()].map(([key, groupStages]) => {
    const seriesStageIds = new Set(
      groupStages.filter(stage => stage.bestOf > 1).map(stage => stage.id)
    );

    const effectiveMatches = seriesStageIds.size === 0
      ? matches
      : [
          ...matches.filter(match => !match.stageId || !seriesStageIds.has(match.stageId)),
          ...[...seriesStageIds].flatMap(stageId =>
            (seriesByStageId.get(stageId) ?? []).map(seriesToRepresentativeMatch)
          ),
        ];

    return {
      bracketName: key === DEFAULT_BRACKET_KEY ? null : key,
      model: buildBracket(groupStages, effectiveMatches),
    };
  });
}
