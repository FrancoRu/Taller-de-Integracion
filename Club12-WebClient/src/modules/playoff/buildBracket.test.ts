import { describe, expect, it } from 'vitest';
import {
  buildBracket,
  buildBrackets,
  countSeriesWins,
  groupStagesByBracket,
  seriesToRepresentativeMatch,
} from '@/modules/playoff/buildBracket';
import { IStageResponse, StageType } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { ITeamMatchResponse } from '@/modules/team/type/team.d';
import { IMatchSeriesResponse, ISeriesGameResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { GUID } from '@/modules/core/types/types';

const guid = (seed: string): GUID => `${seed}-0000-0000-0000-000000000000` as GUID;

const makeTeam = (
  overrides: Partial<ITeamMatchResponse> & { id: GUID; name: string }
): ITeamMatchResponse => ({
  logoUrl: '',
  score: 0,
  players: [],
  scorers: [],
  ...overrides,
});

const makeStage = (overrides: Partial<IStageResponse> & { id: GUID; stageType: StageType }): IStageResponse => ({
  name: overrides.stageType,
  slug: '',
  isActive: true,
  isElimination: true,
  startDate: '2026-01-01',
  endDate: '2026-01-31',
  divisionId: guid('division'),
  order: 0,
  bestOf: 1,
  roundRobinLegs: 1,
  ...overrides,
});

const makeMatch = (overrides: Partial<IMatchResponse> & { id: GUID; stageId: GUID }): IMatchResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Regular' as IMatchResponse['matchType'],
  slug: '',
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  ...overrides,
});

const makeGame = (
  overrides: Partial<ISeriesGameResponse> & { id: GUID }
): ISeriesGameResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  homeTeamName: 'Home',
  visitorTeamName: 'Visitor',
  homeScore: null,
  visitorScore: null,
  winningTeamName: null,
  isFinished: false,
  matchType: 'Playoff' as ISeriesGameResponse['matchType'],
  gameNumber: 1,
  ...overrides,
});

const makeSeries = (
  overrides: Partial<IMatchSeriesResponse> & { id: GUID; stageId: GUID }
): IMatchSeriesResponse => ({
  homeTeamId: guid('home'),
  homeTeamName: 'Home',
  visitorTeamId: guid('visitor'),
  visitorTeamName: 'Visitor',
  bestOf: 3,
  winningTeamId: null,
  winningTeamName: null,
  games: [],
  ...overrides,
});

describe('groupStagesByBracket', () => {
  it('groups stages with no BracketName into one default group', () => {
    const stageA = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const stageB = makeStage({ id: guid('final'), stageType: StageType.Final });

    const groups = groupStagesByBracket([stageA, stageB]);

    expect(groups.size).toBe(1);
    expect([...groups.values()][0]).toEqual([stageA, stageB]);
  });

  it('separates stages by their admin-defined BracketName', () => {
    const goldStage = makeStage({ id: guid('gold-sf'), stageType: StageType.SemiFinal, bracketName: 'Copa de Oro' });
    const silverStage = makeStage({ id: guid('silver-sf'), stageType: StageType.SemiFinal, bracketName: 'Copa de Plata' });

    const groups = groupStagesByBracket([goldStage, silverStage]);

    expect(groups.size).toBe(2);
    expect(groups.get('Copa de Oro')).toEqual([goldStage]);
    expect(groups.get('Copa de Plata')).toEqual([silverStage]);
  });
});

describe('countSeriesWins', () => {
  it('tallies finished games by matching winner name to home/visitor', () => {
    const series = makeSeries({
      id: guid('series'),
      stageId: guid('sf'),
      homeTeamName: 'Home',
      visitorTeamName: 'Visitor',
      games: [
        makeGame({ id: guid('g1'), isFinished: true, winningTeamName: 'Home', gameNumber: 1 }),
        makeGame({ id: guid('g2'), isFinished: true, winningTeamName: 'Visitor', gameNumber: 2 }),
        makeGame({ id: guid('g3'), isFinished: false, gameNumber: 3 }),
      ],
    });

    expect(countSeriesWins(series)).toEqual({ home: 1, visitor: 1 });
  });
});

describe('seriesToRepresentativeMatch', () => {
  it('shows the game tally as the score once at least one game is finished, without marking a winner mid-series', () => {
    const series = makeSeries({
      id: guid('series'),
      stageId: guid('sf'),
      games: [makeGame({ id: guid('g1'), isFinished: true, winningTeamName: 'Home', gameNumber: 1 })],
    });

    const representative = seriesToRepresentativeMatch(series);

    expect(representative.isFinished).toBe(true);
    expect(representative.homeTeam?.score).toBe(1);
    expect(representative.visitorTeam?.score).toBe(0);
    expect(representative.winningTeamId).toBeNull();
  });

  it('sets the winner once the series has been decided', () => {
    const winnerId = guid('home');
    const series = makeSeries({
      id: guid('series'),
      stageId: guid('sf'),
      homeTeamId: winnerId,
      winningTeamId: winnerId,
      winningTeamName: 'Home',
      games: [
        makeGame({ id: guid('g1'), isFinished: true, winningTeamName: 'Home', gameNumber: 1 }),
        makeGame({ id: guid('g2'), isFinished: true, winningTeamName: 'Home', gameNumber: 2 }),
      ],
    });

    const representative = seriesToRepresentativeMatch(series);

    expect(representative.winningTeamId).toBe(winnerId);
    expect(representative.homeTeam?.score).toBe(2);
  });
});

describe('buildBrackets — multi-bracket + series grouping', () => {
  it('builds one BracketModel per BracketName group', () => {
    const goldStage = makeStage({ id: guid('gold-final'), stageType: StageType.Final, bracketName: 'Copa de Oro' });
    const silverStage = makeStage({ id: guid('silver-final'), stageType: StageType.Final, bracketName: 'Copa de Plata' });
    const goldMatch = makeMatch({ id: guid('m-gold'), stageId: goldStage.id });
    const silverMatch = makeMatch({ id: guid('m-silver'), stageId: silverStage.id });

    const groups = buildBrackets([goldStage, silverStage], [goldMatch, silverMatch]);

    expect(groups).toHaveLength(2);
    expect(groups.map(g => g.bracketName).sort()).toEqual(['Copa de Oro', 'Copa de Plata']);
    expect(groups.find(g => g.bracketName === 'Copa de Oro')?.model.rounds[0].matches).toEqual([goldMatch]);
  });

  it('returns a single group with bracketName null when no stage carries a BracketName', () => {
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final });

    const groups = buildBrackets([finalStage], []);

    expect(groups).toHaveLength(1);
    expect(groups[0].bracketName).toBeNull();
  });

  // Playoff draw & seeding: the public bracket view reads the draw date from
  // the FIRST-ROUND stage's drawnAt, surfaced on the group so the view never
  // needs to read the admin-only audit trail.
  it('exposes the first-round stage drawnAt on the group', () => {
    const sfStage = makeStage({
      id: guid('sf'),
      stageType: StageType.SemiFinal,
      order: 1,
      drawnAt: '2026-05-01T12:00:00Z',
    });
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final, order: 2 });

    const groups = buildBrackets([sfStage, finalStage], []);

    expect(groups[0].drawnAt).toBe('2026-05-01T12:00:00Z');
  });

  it('is null when the first-round stage has not been drawn', () => {
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final });

    const groups = buildBrackets([finalStage], []);

    expect(groups[0].drawnAt).toBeNull();
  });

  it('renders a BestOf>1 round as one node per series instead of one per game', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, bestOf: 3 });
    const series = makeSeries({
      id: guid('series'),
      stageId: sfStage.id,
      games: [
        makeGame({ id: guid('g1'), isFinished: true, winningTeamName: 'Home', gameNumber: 1 }),
        makeGame({ id: guid('g2'), isFinished: true, winningTeamName: 'Home', gameNumber: 2 }),
      ],
    });
    const seriesByStageId = new Map([[sfStage.id, [series]]]);

    const groups = buildBrackets([sfStage], [], seriesByStageId);

    const sfRound = groups[0].model.rounds[0];
    expect(sfRound.matches).toHaveLength(1);
    expect(sfRound.matches[0].id).toBe(series.id);
    expect(sfRound.matches[0].homeTeam?.score).toBe(2);
  });
});

describe('buildBracket — round ordering and grouping', () => {
  it('orders rounds Cuartos -> Semifinal -> Final regardless of input order, and groups matches by stageId', () => {
    const qfStage = makeStage({ id: guid('qf'), stageType: StageType.QuarterFinal, order: 1 });
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 2 });
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final, order: 3 });
    const groupStage = makeStage({ id: guid('group'), stageType: StageType.Group, isElimination: false, order: 0 });

    const unsortedInputStages = [finalStage, groupStage, qfStage, sfStage];

    const qfMatch = makeMatch({ id: guid('m-qf'), stageId: qfStage.id });
    const sfMatch = makeMatch({ id: guid('m-sf'), stageId: sfStage.id });
    const finalMatch = makeMatch({ id: guid('m-final'), stageId: finalStage.id });
    const groupMatch = makeMatch({ id: guid('m-group'), stageId: groupStage.id });

    const model = buildBracket(unsortedInputStages, [qfMatch, sfMatch, finalMatch, groupMatch]);

    expect(model.rounds.map(round => round.stageType)).toEqual([
      StageType.QuarterFinal,
      StageType.SemiFinal,
      StageType.Final,
    ]);
    expect(model.rounds[0].matches).toEqual([qfMatch]);
    expect(model.rounds[1].matches).toEqual([sfMatch]);
    expect(model.rounds[2].matches).toEqual([finalMatch]);
  });

  it('drops Group stages entirely and holds ThirdPlace aside from the main rounds', () => {
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final, order: 3 });
    const thirdPlaceStage = makeStage({ id: guid('third'), stageType: StageType.ThirdPlace, order: 3 });
    const groupStage = makeStage({ id: guid('group'), stageType: StageType.Group, isElimination: false, order: 0 });

    const finalMatch = makeMatch({ id: guid('m-final'), stageId: finalStage.id });
    const thirdMatch = makeMatch({ id: guid('m-third'), stageId: thirdPlaceStage.id });
    const groupMatch = makeMatch({ id: guid('m-group'), stageId: groupStage.id });

    const model = buildBracket([finalStage, thirdPlaceStage, groupStage], [finalMatch, thirdMatch, groupMatch]);

    expect(model.rounds).toHaveLength(1);
    expect(model.rounds[0].stageType).toBe(StageType.Final);
    expect(model.thirdPlace?.stageType).toBe(StageType.ThirdPlace);
    expect(model.thirdPlace?.matches).toEqual([thirdMatch]);
  });
});

describe('buildBracket — TBD slots for unresolved participants', () => {
  it('preserves a null homeTeam/visitorTeam on an unseeded Final match so the view can render TBD', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 1 });
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final, order: 2 });

    const sfMatch = makeMatch({
      id: guid('m-sf'),
      stageId: sfStage.id,
      winningTeamId: null,
    });
    const unseededFinalMatch = makeMatch({
      id: guid('m-final'),
      stageId: finalStage.id,
      homeTeam: null,
      visitorTeam: null,
    });

    const model = buildBracket([sfStage, finalStage], [sfMatch, unseededFinalMatch]);

    const finalRound = model.rounds.find(round => round.stageType === StageType.Final);
    expect(finalRound?.matches[0].homeTeam).toBeNull();
    expect(finalRound?.matches[0].visitorTeam).toBeNull();
  });

  it('still includes a round with an empty matches array when its stage has no match rows yet', () => {
    const qfStage = makeStage({ id: guid('qf'), stageType: StageType.QuarterFinal, order: 1 });
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 2 });

    const model = buildBracket([qfStage, sfStage], []);

    expect(model.rounds).toHaveLength(2);
    expect(model.rounds.find(round => round.stageType === StageType.SemiFinal)?.matches).toEqual(
      []
    );
  });
});

describe('buildBracket — client-side connector inference', () => {
  it('emits one edge when a Cuartos winner appears in exactly one Semifinal match', () => {
    const qfStage = makeStage({ id: guid('qf'), stageType: StageType.QuarterFinal, order: 1 });
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 2 });
    const winnerTeamId = guid('team-a');

    const qfMatch = makeMatch({
      id: guid('m-qf'),
      stageId: qfStage.id,
      winningTeamId: winnerTeamId,
      homeTeam: makeTeam({ id: winnerTeamId, name: 'A', score: 3 }),
      visitorTeam: makeTeam({ id: guid('team-b'), name: 'B', score: 1 }),
    });
    const sfMatch = makeMatch({
      id: guid('m-sf'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: winnerTeamId, name: 'A' }),
      visitorTeam: null,
    });

    const model = buildBracket([qfStage, sfStage], [qfMatch, sfMatch]);

    expect(model.edges).toEqual([{ fromMatchId: qfMatch.id, toMatchId: sfMatch.id }]);
  });
});

describe('buildBracket — graceful degradation on ambiguous inference', () => {
  const qfStage = makeStage({ id: guid('qf'), stageType: StageType.QuarterFinal, order: 1 });
  const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 2 });

  it('emits no edge when the source match has no winningTeamId yet (unplayed)', () => {
    const qfMatch = makeMatch({ id: guid('m-qf'), stageId: qfStage.id, winningTeamId: null });
    const sfMatch = makeMatch({
      id: guid('m-sf'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: guid('team-a'), name: 'A' }),
      visitorTeam: null,
    });

    const model = buildBracket([qfStage, sfStage], [qfMatch, sfMatch]);

    expect(model.edges).toEqual([]);
    expect(model.rounds).toHaveLength(2);
  });

  it('emits no edge when the winner matches zero next-round slots (not yet seeded)', () => {
    const winnerTeamId = guid('team-a');
    const qfMatch = makeMatch({ id: guid('m-qf'), stageId: qfStage.id, winningTeamId: winnerTeamId });
    const sfMatch = makeMatch({
      id: guid('m-sf'),
      stageId: sfStage.id,
      homeTeam: null,
      visitorTeam: null,
    });

    const model = buildBracket([qfStage, sfStage], [qfMatch, sfMatch]);

    expect(model.edges).toEqual([]);
  });

  it('emits no edge when the winner matches more than one next-round slot (data tie)', () => {
    const winnerTeamId = guid('team-a');
    const qfMatch = makeMatch({ id: guid('m-qf'), stageId: qfStage.id, winningTeamId: winnerTeamId });
    const sfMatchOne = makeMatch({
      id: guid('m-sf1'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: winnerTeamId, name: 'A' }),
      visitorTeam: null,
    });
    const sfMatchTwo = makeMatch({
      id: guid('m-sf2'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: winnerTeamId, name: 'A' }),
      visitorTeam: null,
    });

    const model = buildBracket([qfStage, sfStage], [qfMatch, sfMatchOne, sfMatchTwo]);

    expect(model.edges).toEqual([]);
  });

  it('emits no edge when the next round has no matches at all', () => {
    const winnerTeamId = guid('team-a');
    const qfMatch = makeMatch({ id: guid('m-qf'), stageId: qfStage.id, winningTeamId: winnerTeamId });

    const model = buildBracket([qfStage, sfStage], [qfMatch]);

    expect(model.edges).toEqual([]);
    expect(model.rounds.find(round => round.stageType === StageType.SemiFinal)?.matches).toEqual(
      []
    );
  });
});

describe('buildBracket — raw-match tie grouping (home-and-away legs with no MatchSeries)', () => {
  it('collapses two raw matches between the same team pair into one aggregate tie node', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const teamA = guid('2k');
    const teamB = guid('nn');

    const leg1 = makeMatch({
      id: guid('leg1'),
      stageId: sfStage.id,
      matchDate: '2026-06-28T18:00:00Z',
      homeTeam: makeTeam({ id: teamA, name: '2K', score: 41 }),
      visitorTeam: makeTeam({ id: teamB, name: 'NN', score: 64 }),
      isFinished: true,
      winningTeamId: teamB,
      winningTeamName: 'NN',
    });
    const leg2 = makeMatch({
      id: guid('leg2'),
      stageId: sfStage.id,
      matchDate: '2026-07-05T18:00:00Z',
      homeTeam: makeTeam({ id: teamB, name: 'NN', score: 54 }),
      visitorTeam: makeTeam({ id: teamA, name: '2K', score: 57 }),
      isFinished: true,
      winningTeamId: teamA,
      winningTeamName: '2K',
    });

    const model = buildBracket([sfStage], [leg1, leg2]);
    const sfRound = model.rounds[0];

    // One node per pairing, not one per raw Match row.
    expect(sfRound.matches).toHaveLength(1);

    const tie = sfRound.matches[0];
    // Aggregate score summed by team id, not by home/visitor slot (legs swap sides).
    expect(tie.homeTeam?.id).toBe(teamA);
    expect(tie.homeTeam?.score).toBe(41 + 57);
    expect(tie.visitorTeam?.id).toBe(teamB);
    expect(tie.visitorTeam?.score).toBe(64 + 54);
    expect(tie.isFinished).toBe(true);
    // Aggregate: 2K 41+57=98, NN 64+54=118 — NN wins on aggregate despite
    // 2K winning the second leg outright.
    expect(tie.winningTeamId).toBe(teamB);

    // Legs are recorded for the view's per-leg breakdown, in chronological order.
    expect(sfRound.legsByMatchId?.get(tie.id)).toEqual([leg1, leg2]);
  });

  it('does not collapse two matches between different team pairs in the same stage', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const matchOne = makeMatch({
      id: guid('m1'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: guid('a'), name: 'A' }),
      visitorTeam: makeTeam({ id: guid('b'), name: 'B' }),
    });
    const matchTwo = makeMatch({
      id: guid('m2'),
      stageId: sfStage.id,
      homeTeam: makeTeam({ id: guid('c'), name: 'C' }),
      visitorTeam: makeTeam({ id: guid('d'), name: 'D' }),
    });

    const model = buildBracket([sfStage], [matchOne, matchTwo]);

    expect(model.rounds[0].matches).toEqual([matchOne, matchTwo]);
    expect(model.rounds[0].legsByMatchId?.size).toBe(0);
  });

  it('leaves a single match per pairing unaffected (the normal, non-grouped case)', () => {
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final });
    const finalMatch = makeMatch({
      id: guid('m-final'),
      stageId: finalStage.id,
      homeTeam: makeTeam({ id: guid('a'), name: 'A' }),
      visitorTeam: makeTeam({ id: guid('b'), name: 'B' }),
    });

    const model = buildBracket([finalStage], [finalMatch]);

    expect(model.rounds[0].matches).toEqual([finalMatch]);
    expect(model.rounds[0].legsByMatchId?.get(finalMatch.id)).toBeUndefined();
  });

  it('does not group TBD slots or byes together even when several share the same stage', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const tbdOne = makeMatch({ id: guid('tbd1'), stageId: sfStage.id, homeTeam: null, visitorTeam: null });
    const tbdTwo = makeMatch({ id: guid('tbd2'), stageId: sfStage.id, homeTeam: null, visitorTeam: null });

    const model = buildBracket([sfStage], [tbdOne, tbdTwo]);

    expect(model.rounds[0].matches).toEqual([tbdOne, tbdTwo]);
  });

  it('does not require exactly two legs — groups however many rows share the pairing', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const teamA = guid('a');
    const teamB = guid('b');
    const legs = [
      makeMatch({
        id: guid('leg1'),
        stageId: sfStage.id,
        matchDate: '2026-01-01T18:00:00Z',
        homeTeam: makeTeam({ id: teamA, name: 'A', score: 10 }),
        visitorTeam: makeTeam({ id: teamB, name: 'B', score: 8 }),
        isFinished: true,
      }),
      makeMatch({
        id: guid('leg2'),
        stageId: sfStage.id,
        matchDate: '2026-01-08T18:00:00Z',
        homeTeam: makeTeam({ id: teamB, name: 'B', score: 12 }),
        visitorTeam: makeTeam({ id: teamA, name: 'A', score: 9 }),
        isFinished: true,
      }),
      makeMatch({
        id: guid('leg3'),
        stageId: sfStage.id,
        matchDate: '2026-01-15T18:00:00Z',
        homeTeam: makeTeam({ id: teamA, name: 'A', score: 15 }),
        visitorTeam: makeTeam({ id: teamB, name: 'B', score: 5 }),
        isFinished: true,
      }),
    ];

    const model = buildBracket([sfStage], legs);
    const sfRound = model.rounds[0];

    expect(sfRound.matches).toHaveLength(1);
    expect(sfRound.legsByMatchId?.get(sfRound.matches[0].id)).toHaveLength(3);
    // A: 10 + 9 + 15 = 34, B: 8 + 12 + 5 = 25
    expect(sfRound.matches[0].homeTeam?.score).toBe(34);
    expect(sfRound.matches[0].winningTeamId).toBe(teamA);
  });

  it('does not mark the tie finished (or decided) while any leg is still pending', () => {
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal });
    const teamA = guid('a');
    const teamB = guid('b');
    const leg1 = makeMatch({
      id: guid('leg1'),
      stageId: sfStage.id,
      matchDate: '2026-01-01T18:00:00Z',
      homeTeam: makeTeam({ id: teamA, name: 'A', score: 41 }),
      visitorTeam: makeTeam({ id: teamB, name: 'B', score: 64 }),
      isFinished: true,
      winningTeamId: teamB,
      winningTeamName: 'B',
    });
    const leg2 = makeMatch({
      id: guid('leg2'),
      stageId: sfStage.id,
      matchDate: '2026-01-08T18:00:00Z',
      homeTeam: makeTeam({ id: teamB, name: 'B' }),
      visitorTeam: makeTeam({ id: teamA, name: 'A' }),
      isFinished: false,
    });

    const model = buildBracket([sfStage], [leg1, leg2]);
    const tie = model.rounds[0].matches[0];

    expect(tie.isFinished).toBe(false);
    expect(tie.winningTeamId).toBeNull();
  });
});

describe('buildBracket — RoundOf16', () => {
  it('includes a manually-created RoundOf16 stage as the round before Cuartos', () => {
    const r16Stage = makeStage({ id: guid('r16'), stageType: StageType.RoundOf16, order: 1 });
    const qfStage = makeStage({ id: guid('qf'), stageType: StageType.QuarterFinal, order: 2 });
    const sfStage = makeStage({ id: guid('sf'), stageType: StageType.SemiFinal, order: 3 });
    const finalStage = makeStage({ id: guid('final'), stageType: StageType.Final, order: 4 });

    const model = buildBracket(
      [finalStage, sfStage, qfStage, r16Stage],
      []
    );

    expect(model.rounds.map(round => round.stageType)).toEqual([
      StageType.RoundOf16,
      StageType.QuarterFinal,
      StageType.SemiFinal,
      StageType.Final,
    ]);
  });
});

describe('buildBracket — no elimination stages for the division', () => {
  it('returns an empty, valid BracketModel when the division has only a Group stage', () => {
    const groupStage = makeStage({
      id: guid('group'),
      stageType: StageType.Group,
      isElimination: false,
      order: 0,
    });
    const groupMatch = makeMatch({ id: guid('m-group'), stageId: groupStage.id });

    const model = buildBracket([groupStage], [groupMatch]);

    expect(model.rounds).toEqual([]);
    expect(model.thirdPlace).toBeUndefined();
    expect(model.edges).toEqual([]);
  });

  it('returns an empty, valid BracketModel when there are no stages at all', () => {
    const model = buildBracket([], []);

    expect(model).toEqual({ rounds: [], thirdPlace: undefined, edges: [] });
  });
});
