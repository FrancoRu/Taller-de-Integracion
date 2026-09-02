import { GUID } from '@/modules/core/types/types';

/** A tournament entry as offered in the "Torneo" select. */
export interface TournamentOption {
  id: GUID;
  name: string;
}

/**
 * Minimal shape of a season needed to populate the statistics filters: its id,
 * calendar year (when set) and the tournaments it groups. Structurally
 * compatible with `ISeasonResponse`.
 */
export interface SeasonOptionSource {
  id: GUID;
  year: number | null;
  tournaments: TournamentOption[];
}

/**
 * Derives the "Torneo" select options for the statistics filter.
 *
 * - With no season selected, every known tournament is offered (global scope).
 * - With a season selected, only the tournaments grouped under that season are
 *   offered — using the tournaments the season response already embeds.
 * - An unknown or missing selection yields an empty list.
 */
export const deriveTournamentOptions = (
  seasons: SeasonOptionSource[] | null,
  selectedSeasonId: GUID | '',
  allTournaments: TournamentOption[] | null
): TournamentOption[] => {
  if (selectedSeasonId) {
    const season = (seasons ?? []).find(s => s.id === selectedSeasonId);
    return (season?.tournaments ?? []).map(({ id, name }) => ({ id, name }));
  }

  return (allTournaments ?? []).map(({ id, name }) => ({ id, name }));
};

/**
 * Resolves the calendar year of the selected season, used to scope the
 * goleadores ranking by season. Returns an empty string when no season is
 * selected, the season is unknown, or the season has no year set.
 */
export const resolveSeasonYear = (
  seasons: SeasonOptionSource[] | null,
  selectedSeasonId: GUID | ''
): number | '' => {
  if (!selectedSeasonId) return '';
  const season = (seasons ?? []).find(s => s.id === selectedSeasonId);
  return season?.year ?? '';
};

/**
 * Resolves which tournaments the summary cards (Equipos, Partidos,
 * Sanciones) should be scoped to:
 * - a chosen torneo scopes to just that one (most specific);
 * - otherwise a chosen temporada scopes to every tournament it groups;
 * - `null` means unscoped (global counts), the original behavior.
 * A season with zero tournaments correctly resolves to `[]` (scoped to
 * nothing), not `null` (unscoped) — the caller must not conflate the two.
 */
export const resolveScopeTournamentIds = (
  seasons: SeasonOptionSource[] | null,
  selectedSeasonId: GUID | '',
  selectedTournamentId: GUID | ''
): GUID[] | null => {
  if (selectedTournamentId) return [selectedTournamentId];
  if (selectedSeasonId) {
    const season = (seasons ?? []).find(s => s.id === selectedSeasonId);
    return (season?.tournaments ?? []).map(t => t.id);
  }
  return null;
};
