import { describe, expect, it } from 'vitest';
import { medicalRecordKeys } from './queryKeys';
import { GUID } from '@/modules/core/types/types';

describe('medicalRecordKeys', () => {
  const playerId: GUID = '11111111-1111-1111-1111-111111111111';
  const teamId: GUID = '22222222-2222-2222-2222-222222222222';
  const tournamentId: GUID = '33333333-3333-3333-3333-333333333333';

  it('byRegistration() returns the registration-scoped literal in triple order', () => {
    expect(
      medicalRecordKeys.byRegistration(playerId, teamId, tournamentId)
    ).toEqual([
      'medicalRecord',
      'byRegistration',
      playerId,
      teamId,
      tournamentId,
    ]);
  });
});
