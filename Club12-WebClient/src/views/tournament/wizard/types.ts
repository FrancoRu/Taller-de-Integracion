import { GUID } from '@/modules/core/types/types';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

/**
 * Elimination round types the wizard can add to a cup, in typical
 * chronological order. RoundOf16/QuarterFinal/SemiFinal/ThirdPlace/Final
 * are all admin-selectable — the wizard never assumes a fixed shape.
 */
export const ELIMINATION_STAGE_TYPES: StageType[] = [
  StageType.RoundOf16,
  StageType.QuarterFinal,
  StageType.SemiFinal,
  StageType.ThirdPlace,
  StageType.Final,
];

export const BEST_OF_OPTIONS = [1, 3, 5, 7];
export const ROUND_ROBIN_LEGS_OPTIONS = [1, 2, 3];

/** Default points a division awards for a win / a loss (HU-79, e.g. FIBA 2/1). No draw points (HU-70). */
export const DEFAULT_POINTS_FOR_WIN = 2;
export const DEFAULT_POINTS_FOR_LOSS = 1;

/** Spanish display labels for each stage type, shared by the cup editor and the review-step tree preview. */
export const STAGE_TYPE_LABELS: Record<StageType, string> = {
  [StageType.Group]: 'Fase de Grupos',
  [StageType.RoundOf16]: 'Octavos de Final',
  [StageType.QuarterFinal]: 'Cuartos de Final',
  [StageType.SemiFinal]: 'Semifinal',
  [StageType.ThirdPlace]: 'Tercer Puesto',
  [StageType.Final]: 'Final',
};

/**
 * One elimination round within a cup (e.g. "Semifinal, al mejor de 3").
 */
export interface RoundConfig {
  id: string;
  stageType: StageType;
  bestOf: number;
}

/**
 * One named parallel playoff bracket within a zone or the cross-division
 * cup (e.g. an admin-named "Copa de Oro"). The name is always free text —
 * never a hardcoded system value.
 */
export interface CupConfig {
  id: string;
  name: string;
  rounds: RoundConfig[];
}

/**
 * One position-range → playoff-destination row the admin defines for a
 * division (HU-45): the teams finishing between `fromPosition` and
 * `toPosition` (inclusive, 1-based) in the group-stage table qualify for
 * the cup named `destination` (a configured cup's name / BracketName).
 * Ranges within a division must not overlap and must stay within the
 * division's team count; positions left uncovered simply don't advance.
 * `id` is a local React key only — never sent to the API.
 */
export interface PlayoffMappingConfig {
  id: string;
  fromPosition: number;
  toPosition: number;
  destination: string;
}

/**
 * One admin-named zone (division) of the tournament: a subset of the
 * registered teams, an optional group stage, zero or more parallel playoff
 * cups, its own points-per-win/loss (HU-79), and the position-range →
 * playoff mappings that seed those cups (HU-45).
 */
export interface ZoneConfig {
  id: string;
  name: string;
  teamIds: GUID[];
  hasGroupStage: boolean;
  roundRobinLegs: number;
  cups: CupConfig[];
  pointsForWin: number;
  pointsForLoss: number;
  playoffMappings: PlayoffMappingConfig[];
}

/**
 * The optional cup that draws teams from every zone at once (e.g. an
 * admin-named "Copa Club12"). Structurally identical to a zone, just
 * exempt from the "one team, one zone" rule.
 */
export interface CrossCupConfig {
  enabled: boolean;
  name: string;
  includeAllTeams: boolean;
  teamIds: GUID[];
  hasGroupStage: boolean;
  roundRobinLegs: number;
  cups: CupConfig[];
  pointsForWin: number;
  pointsForLoss: number;
  playoffMappings: PlayoffMappingConfig[];
}

export interface TournamentStepState {
  name: string;
  description: string;
  startDate: string;
  teamRegistrationDeadline: string;
  /**
   * Competitive category (gender) of the tournament (HU-48). Chosen here and
   * threaded onto both the tournament and every division the wizard creates,
   * so a Feminine tournament's zones are not rejected by the backend's
   * category-match rule.
   */
  category: TournamentCategory;
}

export interface WizardState {
  tournament: TournamentStepState;
  selectedTeamIds: GUID[];
  zones: ZoneConfig[];
  crossCup: CrossCupConfig;
}

let localIdCounter = 0;

/** Generates a locally-unique id for wizard list items (React keys only — never sent to the API). */
export const nextLocalId = (): string => {
  localIdCounter += 1;
  return `local-${localIdCounter}`;
};

export const createEmptyRound = (): RoundConfig => ({
  id: nextLocalId(),
  stageType: StageType.Final,
  bestOf: 1,
});

export const createEmptyCup = (): CupConfig => ({
  id: nextLocalId(),
  name: '',
  rounds: [createEmptyRound()],
});

export const createEmptyPlayoffMapping = (): PlayoffMappingConfig => ({
  id: nextLocalId(),
  fromPosition: 1,
  toPosition: 1,
  destination: '',
});

export const createEmptyZone = (): ZoneConfig => ({
  id: nextLocalId(),
  name: '',
  teamIds: [],
  hasGroupStage: true,
  roundRobinLegs: 1,
  cups: [],
  pointsForWin: DEFAULT_POINTS_FOR_WIN,
  pointsForLoss: DEFAULT_POINTS_FOR_LOSS,
  playoffMappings: [],
});

export const createInitialWizardState = (): WizardState => ({
  tournament: {
    name: '',
    description: '',
    startDate: '',
    teamRegistrationDeadline: '',
    category: TournamentCategory.Masculine,
  },
  selectedTeamIds: [],
  zones: [],
  crossCup: {
    enabled: false,
    name: '',
    includeAllTeams: true,
    teamIds: [],
    hasGroupStage: true,
    roundRobinLegs: 1,
    cups: [],
    pointsForWin: DEFAULT_POINTS_FOR_WIN,
    pointsForLoss: DEFAULT_POINTS_FOR_LOSS,
    playoffMappings: [],
  },
});
