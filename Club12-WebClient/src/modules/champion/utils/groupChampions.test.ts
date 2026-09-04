import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import { groupChampions } from './groupChampions';

const guid = (value: string) => value as GUID;

const entry = (overrides: Partial<IChampionHistory> = {}): IChampionHistory => ({
  tournamentId: guid('tournament-1'),
  tournamentName: 'Apertura 2025',
  seasonName: 'Temporada 2025',
  seasonYear: 2025,
  category: TournamentCategory.Masculine,
  divisionName: 'Zona A',
  cupName: null,
  championTeam: {
    teamId: guid('team-1'),
    teamName: 'Los Halcones',
    logoUrl: null,
  },
  ...overrides,
});

describe('groupChampions', () => {
  it('returns an empty array for empty history', () => {
    expect(groupChampions([])).toEqual([]);
  });

  it('orders seasons by year, newest first', () => {
    const result = groupChampions([
      entry({ seasonName: 'Temporada 2025', seasonYear: 2025 }),
      entry({ seasonName: 'Temporada 2026', seasonYear: 2026 }),
      entry({ seasonName: 'Temporada 2024', seasonYear: 2024 }),
    ]);

    expect(result.map(season => season.seasonName)).toEqual([
      'Temporada 2026',
      'Temporada 2025',
      'Temporada 2024',
    ]);
    expect(result.map(season => season.seasonYear)).toEqual([2026, 2025, 2024]);
  });

  it('sorts null-year seasons (including "Sin temporada") last', () => {
    const result = groupChampions([
      entry({ seasonName: null, seasonYear: null }),
      entry({ seasonName: 'Temporada 2025', seasonYear: 2025 }),
    ]);

    expect(result.map(season => season.seasonName)).toEqual([
      'Temporada 2025',
      'Sin temporada',
    ]);
  });

  it('buckets null or empty seasons under "Sin temporada"', () => {
    const result = groupChampions([
      entry({ seasonName: null, seasonYear: null }),
      entry({ seasonName: '', seasonYear: null }),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0].seasonName).toBe('Sin temporada');
  });

  it('nests Season -> Tournament -> Division, carrying the category on the tournament', () => {
    const result = groupChampions([
      entry({
        tournamentId: guid('t-masc'),
        tournamentName: 'Apertura Masculino',
        category: TournamentCategory.Masculine,
        divisionName: 'Zona A',
      }),
      entry({
        tournamentId: guid('t-fem'),
        tournamentName: 'Apertura Femenino',
        category: TournamentCategory.Feminine,
        divisionName: 'Zona Única',
      }),
      entry({
        tournamentId: guid('t-masc'),
        tournamentName: 'Apertura Masculino',
        category: TournamentCategory.Masculine,
        divisionName: 'Zona B',
      }),
    ]);

    expect(result).toHaveLength(1);
    const [season] = result;
    expect(season.tournaments.map(t => t.tournamentName)).toEqual([
      'Apertura Masculino',
      'Apertura Femenino',
    ]);

    const masc = season.tournaments.find(t => t.tournamentId === guid('t-masc'));
    expect(masc?.category).toBe(TournamentCategory.Masculine);
    expect(masc?.divisions.map(d => d.divisionName)).toEqual(['Zona A', 'Zona B']);
  });

  it('keeps every sub-cup champion of a division, in backend (tier) order', () => {
    const result = groupChampions([
      entry({ divisionName: 'Primera', cupName: 'Copa Oro', championTeam: {
        teamId: guid('gold'), teamName: 'Oro FC', logoUrl: null,
      } }),
      entry({ divisionName: 'Primera', cupName: 'Copa Plata', championTeam: {
        teamId: guid('silver'), teamName: 'Plata FC', logoUrl: null,
      } }),
    ]);

    const division = result[0].tournaments[0].divisions[0];
    expect(division.divisionName).toBe('Primera');
    expect(division.entries.map(e => e.cupName)).toEqual(['Copa Oro', 'Copa Plata']);
  });
});
