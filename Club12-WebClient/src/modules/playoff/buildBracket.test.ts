import { describe, expect, it } from 'vitest';
import { buildBracket } from '@/modules/playoff/buildBracket';
import { IStageResponse, StageType } from '@/modules/stage/type/stage.d';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { ITeamMatchResponse } from '@/modules/team/type/team.d';
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
  isActive: true,
  isElimination: true,
  startDate: '2026-01-01',
  endDate: '2026-01-31',
  divisionId: guid('division'),
  order: 0,
  ...overrides,
});

const makeMatch = (overrides: Partial<IMatchResponse> & { id: GUID; stageId: GUID }): IMatchResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Regular' as IMatchResponse['matchType'],
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  ...overrides,
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
