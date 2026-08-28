/**
 * Competitive category (gender) of a tournament (HU-48). The feminine
 * competition is, by club rule, a SEPARATE tournament: a single tournament can
 * never mix feminine and masculine divisions. The category lives on the
 * tournament and every one of its divisions must share it.
 *
 * Values mirror the backend `Domain.Enums.TournamentCategory` names exactly
 * (the API serializes enums as strings via JsonStringEnumConverter).
 */
export const TournamentCategory = {
  Masculine: 'Masculine',
  Feminine: 'Feminine',
} as const;

export type TournamentCategory =
  (typeof TournamentCategory)[keyof typeof TournamentCategory];

/** Spanish display labels for each category, shared by the wizard and views. */
export const TOURNAMENT_CATEGORY_LABELS: Record<TournamentCategory, string> = {
  [TournamentCategory.Masculine]: 'Masculino',
  [TournamentCategory.Feminine]: 'Femenino',
};
