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
  category: TournamentCategory.Masculine,
  divisionName: 'Zona A',
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

  it('preserves the first-seen order of seasons', () => {
    const result = groupChampions([
      entry({ seasonName: 'Temporada 2024' }),
      entry({ seasonName: 'Temporada 2025' }),
      // A later 2024 entry must not move 2024 after 2025.
      entry({ seasonName: 'Temporada 2024' }),
    ]);

    expect(result.map(season => season.seasonName)).toEqual([
      'Temporada 2024',
      'Temporada 2025',
    ]);
  });

  it('buckets null or empty seasons under "Sin temporada"', () => {
    const result = groupChampions([
      entry({ seasonName: null }),
      entry({ seasonName: '' }),
    ]);

    expect(result).toHaveLength(1);
    expect(result[0].seasonName).toBe('Sin temporada');
    expect(result[0].categories[0].entries).toHaveLength(2);
  });

  it('orders categories masculine first, then feminine', () => {
    const result = groupChampions([
      entry({ category: TournamentCategory.Feminine }),
      entry({ category: TournamentCategory.Masculine }),
    ]);

    expect(result[0].categories.map(c => c.category)).toEqual([
      TournamentCategory.Masculine,
      TournamentCategory.Feminine,
    ]);
  });

  it('only includes categories that actually have entries', () => {
    const result = groupChampions([
      entry({ category: TournamentCategory.Feminine }),
    ]);

    expect(result[0].categories).toHaveLength(1);
    expect(result[0].categories[0].category).toBe(TournamentCategory.Feminine);
  });

  it('groups entries into their season and category buckets', () => {
    const result = groupChampions([
      entry({
        seasonName: 'Temporada 2025',
        category: TournamentCategory.Masculine,
        divisionName: 'Zona A',
      }),
      entry({
        seasonName: 'Temporada 2025',
        category: TournamentCategory.Feminine,
        divisionName: 'Zona Única',
      }),
      entry({
        seasonName: 'Temporada 2025',
        category: TournamentCategory.Masculine,
        divisionName: 'Zona B',
      }),
    ]);

    expect(result).toHaveLength(1);
    const [season] = result;
    expect(season.categories).toHaveLength(2);

    const masculine = season.categories.find(
      c => c.category === TournamentCategory.Masculine
    );
    const feminine = season.categories.find(
      c => c.category === TournamentCategory.Feminine
    );

    expect(masculine?.entries.map(e => e.divisionName)).toEqual([
      'Zona A',
      'Zona B',
    ]);
    expect(feminine?.entries.map(e => e.divisionName)).toEqual(['Zona Única']);
  });
});
