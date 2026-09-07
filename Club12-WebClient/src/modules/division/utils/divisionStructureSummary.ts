import { ITournamentStructureResponse } from '@/modules/tournament/type/tournament.d';
import { structureToWizardState } from '@/views/tournament/wizard/cloneWizard';
import { CrossCupConfig, ZoneConfig } from '@/views/tournament/wizard/types';

/**
 * One division's structure, resolved out of its tournament's full structure
 * tree — a regular zone or the cross-division cup, never both. `review`
 * carries any derivation mismatches found while reconstructing it (the same
 * checks the tournament-cloning reverse-mapper runs), surfaced here as
 * advisory notices rather than silently guessed.
 */
export interface DivisionStructureSummary {
  zone?: ZoneConfig;
  crossCup?: CrossCupConfig;
  review: string[];
}

/**
 * Resolves a single division's structure by name out of its tournament's
 * structure tree, reusing the tournament-cloning reverse-mapper
 * ({@link structureToWizardState}) instead of a second, parallel parser.
 * Returns null when no division in the tournament matches the given name.
 */
export const findDivisionStructure = (
  tournamentStructure: ITournamentStructureResponse,
  divisionName: string
): DivisionStructureSummary | null => {
  const { state, review } = structureToWizardState(
    tournamentStructure,
    tournamentStructure.category
  );

  const zone = state.zones.find(candidate => candidate.name === divisionName);
  if (zone) {
    return { zone, review };
  }

  if (state.crossCup.enabled && state.crossCup.name === divisionName) {
    return { crossCup: state.crossCup, review };
  }

  return null;
};
