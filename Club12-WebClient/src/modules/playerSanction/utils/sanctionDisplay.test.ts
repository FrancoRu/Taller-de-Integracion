import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IPlayerSanctionResponse } from '@/modules/playerSanction/type/playerSanction.d';
import {
  formatFechasRemaining,
  formatSanctionDurationFechas,
  getSanctionStateLabel,
  getSanctionSubjectName,
  getSanctionSubjectTypeLabel,
} from '@/modules/playerSanction/utils/sanctionDisplay';

const guid = (value: string) => value as GUID;

const sanction = (
  overrides: Partial<IPlayerSanctionResponse>
): IPlayerSanctionResponse => ({
  id: guid('sanction-1'),
  duration: 2,
  fechasRemaining: 1,
  isActive: true,
  issuedDate: new Date('2026-08-01T00:00:00Z'),
  description: 'Motivo',
  slug: 'sancion-1',
  subjectType: 'Player',
  playerId: guid('player-1'),
  playerFullName: 'Ana Gómez',
  teamId: null,
  teamName: null,
  staffName: null,
  matchId: guid('match-1'),
  appealStatus: 'None',
  ...overrides,
});

describe('getSanctionSubjectName (HU-77)', () => {
  it('returns the player full name for a player sanction', () => {
    expect(getSanctionSubjectName(sanction({ subjectType: 'Player' }))).toBe(
      'Ana Gómez'
    );
  });

  it('returns the team name for a team sanction', () => {
    const row = sanction({
      subjectType: 'Team',
      playerFullName: null,
      teamName: 'Club 12',
    });
    expect(getSanctionSubjectName(row)).toBe('Club 12');
    expect(getSanctionSubjectTypeLabel(row)).toBe('Equipo');
  });

  it('returns the staff name for a staff sanction', () => {
    const row = sanction({
      subjectType: 'Staff',
      playerFullName: null,
      staffName: 'Coordinador X',
    });
    expect(getSanctionSubjectName(row)).toBe('Coordinador X');
    expect(getSanctionSubjectTypeLabel(row)).toBe('Staff');
  });
});

describe('sanction duration labels (HU-75)', () => {
  it('labels the duration in fechas and never in días', () => {
    const label = formatSanctionDurationFechas(2);
    expect(label).toBe('2 fechas');
    expect(label).not.toMatch(/día/i);
  });

  it('uses the singular "fecha" for a one-fecha sanction', () => {
    expect(formatSanctionDurationFechas(1)).toBe('1 fecha');
  });

  it('shows "Permanente" for indefinite bans', () => {
    expect(formatSanctionDurationFechas(999)).toBe('Permanente');
  });

  it('formats fechas remaining, using an em dash when unknown', () => {
    expect(formatFechasRemaining(0)).toBe('0 fechas');
    expect(formatFechasRemaining(1)).toBe('1 fecha');
    expect(formatFechasRemaining(null)).toBe('—');
    expect(formatFechasRemaining(undefined)).toBe('—');
    expect(formatFechasRemaining(3)).not.toMatch(/día/i);
  });
});

describe('getSanctionStateLabel (HU-75/HU-76)', () => {
  it('reports active and served states', () => {
    expect(getSanctionStateLabel({ isActive: true })).toBe('Activa');
    expect(getSanctionStateLabel({ isActive: false })).toBe('Cumplida');
  });
});
