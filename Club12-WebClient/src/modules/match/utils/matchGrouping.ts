import { IMatchResponse, IRoundMatchesResponse } from '@/modules/match/type/match';

/**
 * The label used for a team that sits out a matchday (bye). With an odd number
 * of teams each round leaves exactly one team free (HU-65).
 */
export const BYE_TEAM_LABEL = 'Libre';

/** Sort key placing the null round (knockout matches) after the numbered ones. */
const roundSortKey = (round: number | null): number =>
  round ?? Number.MAX_SAFE_INTEGER;

/**
 * Groups matches by their matchday (jornada / round) and orders the groups
 * ascending — the canonical fixture rendering for HU-63/HU-65 ("Fecha 1",
 * "Fecha 2", …). Matches with a null round (e.g. knockout stages) are collected
 * into a single trailing group. Grouping is by the round number, never by the
 * calendar date.
 */
export const groupMatchesByRound = (
  matches: IMatchResponse[]
): IRoundMatchesResponse[] => {
  const byRound = new Map<number | null, IMatchResponse[]>();

  matches.forEach(match => {
    const round = match.round ?? null;
    const group = byRound.get(round) ?? [];
    group.push(match);
    byRound.set(round, group);
  });

  return Array.from(byRound.entries())
    .sort(([a], [b]) => roundSortKey(a) - roundSortKey(b))
    .map(([round, roundMatches]) => ({
      round,
      matches: [...roundMatches].sort((a, b) =>
        a.matchDate.localeCompare(b.matchDate)
      ),
    }));
};

/**
 * The human-readable header for a round: "Fecha 1", "Fecha 2", … for numbered
 * jornadas, and a generic label for the null (knockout) group.
 */
export const formatRoundLabel = (round: number | null): string =>
  round == null ? 'Fase final' : `Fecha ${round}`;

/** The name of each side of a match that is actually a team (skips byes/TBD). */
const matchTeamNames = (match: IMatchResponse): string[] =>
  [match.homeTeam?.name, match.visitorTeam?.name].filter(
    (name): name is string => Boolean(name)
  );

/**
 * The distinct team names appearing across every match of a stage. Used as the
 * roster to derive which team is free ("Libre") on a given matchday.
 */
export const collectStageTeamNames = (matches: IMatchResponse[]): string[] => {
  const names = new Set<string>();
  matches.forEach(match => matchTeamNames(match).forEach(name => names.add(name)));
  return Array.from(names);
};

/**
 * The teams sitting out a given round (bye / "Libre", HU-65): a stage team that
 * plays in some round but has no match in this one. Derivable purely from the
 * fixture — no extra endpoint — as long as the full roster is known. Returns an
 * empty list when the data can't support the derivation (e.g. a knockout round
 * where absence just means "not drawn yet").
 */
export const byeTeamNamesForRound = (
  roundMatches: IMatchResponse[],
  stageTeamNames: string[]
): string[] => {
  const playing = new Set<string>();
  roundMatches.forEach(match =>
    matchTeamNames(match).forEach(name => playing.add(name))
  );

  return stageTeamNames.filter(name => !playing.has(name));
};
