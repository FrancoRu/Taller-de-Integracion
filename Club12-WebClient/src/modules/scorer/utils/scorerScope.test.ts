import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { buildScorerScopeParams } from '@/modules/scorer/utils/scorerScope';

const tournamentId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' as GUID;
const divisionId = '11111111-2222-3333-4444-555555555555' as GUID;
const stageId = '66666666-7777-8888-9999-000000000000' as GUID;

describe('buildScorerScopeParams', () => {
  it('tournament scope sends tournamentId (+ division/stage) and no season', () => {
    const params = buildScorerScopeParams('tournament', {
      tournamentId,
      divisionId,
      stageId,
      season: 2026,
    });

    expect(params).toEqual({ tournamentId, divisionId, stageId });
    expect(params.season).toBeUndefined();
  });

  it('tournament scope drops empty division/stage to undefined', () => {
    const params = buildScorerScopeParams('tournament', {
      tournamentId,
      divisionId: '',
      stageId: '',
    });

    expect(params).toEqual({
      tournamentId,
      divisionId: undefined,
      stageId: undefined,
    });
  });

  it('season scope sends only the season year, no tournamentId', () => {
    const params = buildScorerScopeParams('season', {
      tournamentId,
      season: 2026,
    });

    expect(params).toEqual({ season: 2026 });
    expect(params.tournamentId).toBeUndefined();
  });

  it('season scope with no year selected sends an undefined season', () => {
    const params = buildScorerScopeParams('season', { season: '' });

    expect(params).toEqual({ season: undefined });
  });

  it('allTime scope sends neither tournamentId nor season', () => {
    const params = buildScorerScopeParams('allTime', {
      tournamentId,
      season: 2026,
    });

    expect(params).toEqual({});
  });
});
