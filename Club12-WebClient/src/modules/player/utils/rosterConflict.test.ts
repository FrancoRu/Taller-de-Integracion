import { AxiosError, AxiosHeaders } from 'axios';
import { describe, expect, it } from 'vitest';
import {
  ROSTER_CONFLICT_MESSAGES,
  mapRosterConflictMessage,
} from '@/modules/player/utils/rosterConflict';

const conflict = (detail: string): AxiosError => {
  const error = new AxiosError('Conflict');
  error.response = {
    status: 409,
    statusText: 'Conflict',
    data: { detail },
    headers: {},
    config: { headers: new AxiosHeaders() },
  };
  return error;
};

describe('mapRosterConflictMessage (HU-54)', () => {
  it('maps a duplicate dorsal 409 to a clear message', () => {
    const message = mapRosterConflictMessage(
      conflict("Jersey number 10 is already used by another player in team 'x'.")
    );

    expect(message).toBe(ROSTER_CONFLICT_MESSAGES.duplicateDorsal);
  });

  it('maps a roster-full 409 to a clear message', () => {
    const message = mapRosterConflictMessage(
      conflict("Team 'x' already has the maximum of 12 players for this tournament.")
    );

    expect(message).toBe(ROSTER_CONFLICT_MESSAGES.rosterFull);
  });

  it('maps a player-already-in-another-team 409 to a clear message', () => {
    const message = mapRosterConflictMessage(
      conflict(
        "Player 'p' is already registered to another team in tournament 't'. " +
          'A player cannot be registered to two teams in the same tournament.'
      )
    );

    expect(message).toBe(ROSTER_CONFLICT_MESSAGES.alreadyInAnotherTeam);
  });

  it('falls back to a generic message for a non-conflict error', () => {
    const error = new AxiosError('Boom');
    error.response = {
      status: 500,
      statusText: 'Server Error',
      data: {},
      headers: {},
      config: { headers: new AxiosHeaders() },
    };

    expect(mapRosterConflictMessage(error)).toBe(
      ROSTER_CONFLICT_MESSAGES.generic
    );
  });
});
