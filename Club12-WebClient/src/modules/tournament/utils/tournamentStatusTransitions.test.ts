import { describe, expect, it } from 'vitest';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import {
  getNextStatusOptions,
  isValidStatusTransition,
  isTerminalStatus,
} from './tournamentStatusTransitions';

describe('getNextStatusOptions', () => {
  it('offers OpenForRegistration or Canceled from Scheduled', () => {
    expect(getNextStatusOptions(TournamentStatus.Scheduled)).toEqual([
      TournamentStatus.OpenForRegistration,
      TournamentStatus.Canceled,
    ]);
  });

  it('offers RegistrationClosed or Canceled from OpenForRegistration', () => {
    expect(getNextStatusOptions(TournamentStatus.OpenForRegistration)).toEqual([
      TournamentStatus.RegistrationClosed,
      TournamentStatus.Canceled,
    ]);
  });

  it('offers Ongoing or Canceled from RegistrationClosed', () => {
    expect(getNextStatusOptions(TournamentStatus.RegistrationClosed)).toEqual([
      TournamentStatus.Ongoing,
      TournamentStatus.Canceled,
    ]);
  });

  it('offers Finished or Canceled from Ongoing', () => {
    expect(getNextStatusOptions(TournamentStatus.Ongoing)).toEqual([
      TournamentStatus.Finished,
      TournamentStatus.Canceled,
    ]);
  });

  it('offers nothing from the terminal statuses', () => {
    expect(getNextStatusOptions(TournamentStatus.Finished)).toEqual([]);
    expect(getNextStatusOptions(TournamentStatus.Canceled)).toEqual([]);
  });
});

describe('isValidStatusTransition', () => {
  it('accepts each forward step of the happy path', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.Scheduled,
        TournamentStatus.OpenForRegistration
      )
    ).toBe(true);
    expect(
      isValidStatusTransition(
        TournamentStatus.OpenForRegistration,
        TournamentStatus.RegistrationClosed
      )
    ).toBe(true);
    expect(
      isValidStatusTransition(
        TournamentStatus.RegistrationClosed,
        TournamentStatus.Ongoing
      )
    ).toBe(true);
    expect(
      isValidStatusTransition(
        TournamentStatus.Ongoing,
        TournamentStatus.Finished
      )
    ).toBe(true);
  });

  it('treats a no-op transition to the same status as valid', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.Ongoing,
        TournamentStatus.Ongoing
      )
    ).toBe(true);
  });

  it('rejects skipping a step', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.Scheduled,
        TournamentStatus.RegistrationClosed
      )
    ).toBe(false);
    expect(
      isValidStatusTransition(
        TournamentStatus.OpenForRegistration,
        TournamentStatus.Ongoing
      )
    ).toBe(false);
  });

  it('rejects moving backward', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.RegistrationClosed,
        TournamentStatus.OpenForRegistration
      )
    ).toBe(false);
  });

  it('rejects leaving a terminal status', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.Finished,
        TournamentStatus.Ongoing
      )
    ).toBe(false);
    expect(
      isValidStatusTransition(
        TournamentStatus.Canceled,
        TournamentStatus.Scheduled
      )
    ).toBe(false);
  });

  it('allows cancelling from any non-terminal status', () => {
    expect(
      isValidStatusTransition(
        TournamentStatus.Scheduled,
        TournamentStatus.Canceled
      )
    ).toBe(true);
    expect(
      isValidStatusTransition(
        TournamentStatus.RegistrationClosed,
        TournamentStatus.Canceled
      )
    ).toBe(true);
  });
});

describe('isTerminalStatus', () => {
  it('is true only for Finished and Canceled', () => {
    expect(isTerminalStatus(TournamentStatus.Finished)).toBe(true);
    expect(isTerminalStatus(TournamentStatus.Canceled)).toBe(true);
    expect(isTerminalStatus(TournamentStatus.Scheduled)).toBe(false);
    expect(isTerminalStatus(TournamentStatus.RegistrationClosed)).toBe(false);
  });
});
