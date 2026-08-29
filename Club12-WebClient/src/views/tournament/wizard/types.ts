import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';

export const BEST_OF_OPTIONS = [1, 3, 5, 7];
export const ROUND_ROBIN_LEGS_OPTIONS = [1, 2, 3];

/** Default series format for a playoff round when none is chosen (best-of-3). */
export const DEFAULT_BEST_OF = 3;

/** A playoff cup needs at least 2 qualifiers (a final), and at most 16 (the deepest bracket the stage types model — Octavos). */
export const MIN_CUP_QUALIFIERS = 2;
export const MAX_CUP_QUALIFIERS = 16;

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
 * HU-112: derives a cup's bracket rounds from how many teams qualify to it,
 * so the rounds ALWAYS match the qualifier count (you can never configure
 * "4 qualify" with only a final). Non-powers-of-two are padded with byes by
 * the backend seeder, so the depth here is `ceil(log2(qualifiers))` rounds:
 * 2 → Final; 3-4 → Semifinal+Final; 5-8 → Cuartos+Semifinal+Final;
 * 9-16 → Octavos+Cuartos+Semifinal+Final.
 */
export const qualifiersToStageTypes = (qualifiers: number): StageType[] => {
  if (qualifiers <= 2) return [StageType.Final];
  if (qualifiers <= 4) return [StageType.SemiFinal, StageType.Final];
  if (qualifiers <= 8) {
    return [StageType.QuarterFinal, StageType.SemiFinal, StageType.Final];
  }
  return [
    StageType.RoundOf16,
    StageType.QuarterFinal,
    StageType.SemiFinal,
    StageType.Final,
  ];
};

/**
 * One named parallel playoff bracket within a zone or the cross-division
 * cup (e.g. an admin-named "Copa de Oro"). The name is always free text —
 * never a hardcoded system value.
 *
 * HU-112: a cup is defined by HOW MANY teams qualify to it (`qualifiers`)
 * and the series format (`bestOf`). The bracket rounds are DERIVED from the
 * qualifier count (see {@link qualifiersToStageTypes}) so the two can never
 * be inconsistent, and the zone's standings→cup position ranges are derived
 * from the cups' order (top cup gets positions 1..q0, next q0+1..q0+q1, …).
 */
export interface CupConfig {
  id: string;
  name: string;
  /** How many teams qualify to this cup from the group-stage standings (HU-112, ≥ 2). */
  qualifiers: number;
  /**
   * Series format PER derived bracket phase (HU-112): a stage type → best-of
   * (1 single game, 3/5/7 best-of-N). Phases are derived from `qualifiers`
   * ({@link qualifiersToStageTypes}); a phase missing here defaults to
   * {@link DEFAULT_BEST_OF}, so semis can be best-of-3 while the final is
   * best-of-5.
   */
  bestOfByStage: Partial<Record<StageType, number>>;
}

/** The best-of a cup's derived phase uses, defaulting to {@link DEFAULT_BEST_OF}. */
export const getStageBestOf = (cup: CupConfig, stageType: StageType): number =>
  cup.bestOfByStage[stageType] ?? DEFAULT_BEST_OF;

/**
 * The standings positions that qualify to the cup at `index`, given the cups'
 * order (HU-112): cups fill top-down, so cup 0 takes 1..q0, cup 1 takes
 * q0+1..q0+q1, and so on. Cups without a name are skipped (they seed nothing).
 */
export const cupPositionRange = (
  cups: CupConfig[],
  index: number
): { from: number; to: number } => {
  let from = 1;
  for (let i = 0; i < index; i += 1) {
    if (cups[i].name.trim().length > 0) {
      from += cups[i].qualifiers;
    }
  }
  return { from, to: from + cups[index].qualifiers - 1 };
};

/**
 * One admin-named zone (division) of the tournament: an optional group
 * stage, zero or more parallel playoff cups, and its own points-per-win/loss
 * (HU-79). The wizard defines STRUCTURE ONLY (HU-106): teams are added later,
 * during the registration phase, and assigned to divisions once registration
 * closes (HU-107/108). Each cup's standings→cup position ranges (HU-45) are
 * derived from the cups' order and qualifier counts (HU-112).
 */
export interface ZoneConfig {
  id: string;
  name: string;
  hasGroupStage: boolean;
  roundRobinLegs: number;
  cups: CupConfig[];
  pointsForWin: number;
  pointsForLoss: number;
}

/**
 * The optional cup that spans every zone at once (e.g. an admin-named
 * "Copa Club12"). Flagged as a cross-division cup so it is exempt from the
 * "one team, one zone" rule when teams are eventually assigned (HU-107/108).
 *
 * HU-110: the cross cup is a MULTI-GROUP competition — it is split into
 * `groupCount` group stages ("Grupo 1"…"Grupo N"), and the top
 * `qualifiersPerGroup` teams of every group are pooled into a single
 * knockout bracket. Its bracket rounds are derived from the pooled total
 * (`groupCount * qualifiersPerGroup`, HU-112). The wizard defines its
 * STRUCTURE ONLY (HU-106) — no teams are selected here.
 */
export interface CrossCupConfig {
  enabled: boolean;
  name: string;
  /** How many group stages the cross cup is split into (HU-110, >= 1). */
  groupCount: number;
  /** How many teams advance from each group to the bracket (HU-110, >= 1). */
  qualifiersPerGroup: number;
  roundRobinLegs: number;
  cups: CupConfig[];
  pointsForWin: number;
  pointsForLoss: number;
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
  /**
   * Optional season ("Temporada") the tournament is grouped under. Empty string
   * means "no season". Threaded onto the addTournament payload as `seasonId`.
   */
  seasonId?: string;
}

export interface WizardState {
  tournament: TournamentStepState;
  zones: ZoneConfig[];
  crossCup: CrossCupConfig;
}

let localIdCounter = 0;

/** Generates a locally-unique id for wizard list items (React keys only — never sent to the API). */
export const nextLocalId = (): string => {
  localIdCounter += 1;
  return `local-${localIdCounter}`;
};

export const createEmptyCup = (): CupConfig => ({
  id: nextLocalId(),
  name: '',
  // Default: top 4 qualify (semis + final). Each derived phase defaults to
  // best-of-3 (DEFAULT_BEST_OF) — the club's typical cup.
  qualifiers: 4,
  bestOfByStage: {},
});

export const createEmptyZone = (): ZoneConfig => ({
  id: nextLocalId(),
  name: '',
  hasGroupStage: true,
  roundRobinLegs: 1,
  cups: [],
  pointsForWin: DEFAULT_POINTS_FOR_WIN,
  pointsForLoss: DEFAULT_POINTS_FOR_LOSS,
});

export const createInitialWizardState = (): WizardState => ({
  tournament: {
    name: '',
    description: '',
    startDate: '',
    teamRegistrationDeadline: '',
    category: TournamentCategory.Masculine,
    seasonId: '',
  },
  zones: [],
  crossCup: {
    enabled: false,
    name: '',
    groupCount: 1,
    qualifiersPerGroup: 1,
    roundRobinLegs: 1,
    cups: [],
    pointsForWin: DEFAULT_POINTS_FOR_WIN,
    pointsForLoss: DEFAULT_POINTS_FOR_LOSS,
  },
});
