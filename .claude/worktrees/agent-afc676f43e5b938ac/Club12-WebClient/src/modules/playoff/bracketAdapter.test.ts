import { describe, expect, it } from 'vitest';
import { toLibraryMatches, libraryRoundLabels } from '@/modules/playoff/bracketAdapter';
import { BracketModel } from '@/modules/playoff/type/bracket.d';
import { StageType } from '@/modules/stage/type/stage';
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

const makeMatch = (overrides: Partial<IMatchResponse> & { id: GUID; stageId: GUID }): IMatchResponse => ({
  matchDate: '2026-01-01T18:00:00Z',
  matchType: 'Playoff' as IMatchResponse['matchType'],
  slug: '',
  homeTeam: null,
  visitorTeam: null,
  isFinished: false,
  winningTeamId: null,
  winningTeamName: null,
  venue: null,
  ...overrides,
});

describe('toLibraryMatches', () => {
  it('maps an unambiguous inferred edge to nextMatchId', () => {
    const teamA = guid('team-a');
    const qfMatch = makeMatch({
      id: guid('m-qf'),
      stageId: guid('qf'),
      winningTeamId: teamA,
      homeTeam: makeTeam({ id: teamA, name: 'A', score: 3 }),
      visitorTeam: makeTeam({ id: guid('team-b'), name: 'B', score: 1 }),
      isFinished: true,
    });
    const sfMatch = makeMatch({
      id: guid('m-sf'),
      stageId: guid('sf'),
      homeTeam: makeTeam({ id: teamA, name: 'A' }),
      visitorTeam: null,
    });

    const model: BracketModel = {
      rounds: [
        { stageId: guid('qf'), stageType: StageType.QuarterFinal, matches: [qfMatch] },
        { stageId: guid('sf'), stageType: StageType.SemiFinal, matches: [sfMatch] },
      ],
      edges: [{ fromMatchId: qfMatch.id, toMatchId: sfMatch.id }],
    };

    const matches = toLibraryMatches(model);

    expect(matches.find(m => m.id === qfMatch.id)?.nextMatchId).toBe(sfMatch.id);
  });

  it('falls back to positional pairing (index / 2) when no edge was inferred yet', () => {
    const sfMatchA = makeMatch({ id: guid('m-sf-a'), stageId: guid('sf') });
    const sfMatchB = makeMatch({ id: guid('m-sf-b'), stageId: guid('sf') });
    const finalMatch = makeMatch({ id: guid('m-final'), stageId: guid('final') });

    const model: BracketModel = {
      rounds: [
        { stageId: guid('sf'), stageType: StageType.SemiFinal, matches: [sfMatchA, sfMatchB] },
        { stageId: guid('final'), stageType: StageType.Final, matches: [finalMatch] },
      ],
      edges: [],
    };

    const matches = toLibraryMatches(model);

    expect(matches.find(m => m.id === sfMatchA.id)?.nextMatchId).toBe(finalMatch.id);
    expect(matches.find(m => m.id === sfMatchB.id)?.nextMatchId).toBe(finalMatch.id);
  });

  it('sets nextMatchId to null for the last round', () => {
    const finalMatch = makeMatch({ id: guid('m-final'), stageId: guid('final') });

    const model: BracketModel = {
      rounds: [{ stageId: guid('final'), stageType: StageType.Final, matches: [finalMatch] }],
      edges: [],
    };

    const matches = toLibraryMatches(model);

    expect(matches[0].nextMatchId).toBeNull();
  });

  it('excludes thirdPlace from the library matches entirely', () => {
    const finalMatch = makeMatch({ id: guid('m-final'), stageId: guid('final') });
    const thirdMatch = makeMatch({ id: guid('m-third'), stageId: guid('third') });

    const model: BracketModel = {
      rounds: [{ stageId: guid('final'), stageType: StageType.Final, matches: [finalMatch] }],
      thirdPlace: { stageId: guid('third'), stageType: StageType.ThirdPlace, matches: [thirdMatch] },
      edges: [],
    };

    const matches = toLibraryMatches(model);

    expect(matches).toHaveLength(1);
    expect(matches.find(m => m.id === thirdMatch.id)).toBeUndefined();
  });

  it('skips rounds with zero matches so round numbering stays contiguous', () => {
    const finalMatch = makeMatch({ id: guid('m-final'), stageId: guid('final') });

    const model: BracketModel = {
      rounds: [
        { stageId: guid('qf'), stageType: StageType.QuarterFinal, matches: [] },
        { stageId: guid('final'), stageType: StageType.Final, matches: [finalMatch] },
      ],
      edges: [],
    };

    const matches = toLibraryMatches(model);

    expect(matches).toHaveLength(1);
    expect(libraryRoundLabels(model)).toEqual(['FINAL']);
  });

  it('synthesizes one placeholder match for a later round whose stage exists but has no visible match rows yet, so every earlier match still has exactly one place to connect into', () => {
    // Regression test: SingleEliminationBracket only renders one match when
    // more than one match has nextMatchId: null (a "multi-root" tree) —
    // confirmed against the installed library in manual testing. A later
    // round with zero matches (e.g. the Final stage exists but hasn't been
    // seeded yet, or got cut off by an upstream paginated fetch) must never
    // leave more than one match in the whole bracket with a null
    // nextMatchId, or every match but one silently disappears.
    const sfMatchA = makeMatch({ id: guid('m-sf-a'), stageId: guid('sf') });
    const sfMatchB = makeMatch({ id: guid('m-sf-b'), stageId: guid('sf') });
    const finalStageId = guid('final');

    const model: BracketModel = {
      rounds: [
        { stageId: guid('sf'), stageType: StageType.SemiFinal, matches: [sfMatchA, sfMatchB] },
        { stageId: finalStageId, stageType: StageType.Final, matches: [] },
      ],
      edges: [],
    };

    const matches = toLibraryMatches(model);

    // Both semifinal matches survive...
    expect(matches.map(m => m.id)).toEqual(
      expect.arrayContaining([sfMatchA.id, sfMatchB.id])
    );
    // ...exactly one match is the root (nextMatchId: null)...
    expect(matches.filter(m => m.nextMatchId === null)).toHaveLength(1);
    // ...and both SF matches converge into that same synthesized placeholder.
    expect(matches.find(m => m.id === sfMatchA.id)?.nextMatchId).toBe(finalStageId);
    expect(matches.find(m => m.id === sfMatchB.id)?.nextMatchId).toBe(finalStageId);
    expect(libraryRoundLabels(model)).toEqual(['SEMIFINAL', 'FINAL']);
  });

  it('carries the raw match and per-side participant ids for a TBD slot', () => {
    const finalMatch = makeMatch({
      id: guid('m-final'),
      stageId: guid('final'),
      homeTeam: null,
      visitorTeam: null,
    });

    const model: BracketModel = {
      rounds: [{ stageId: guid('final'), stageType: StageType.Final, matches: [finalMatch] }],
      edges: [],
    };

    const [match] = toLibraryMatches(model);

    expect(match.raw).toBe(finalMatch);
    expect(match.participants.map(p => p.id)).toEqual([
      `${finalMatch.id}:home`,
      `${finalMatch.id}:visitor`,
    ]);
  });
});
