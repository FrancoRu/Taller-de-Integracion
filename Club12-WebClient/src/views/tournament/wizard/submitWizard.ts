import { GUID } from '@/modules/core/types/types';
import { ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import {
  ICreateFullDivisionRequest,
  ICreateFullStageRequest,
  ICreateFullTournamentRequest,
} from '@/modules/tournament/type/createFullTournament.d';
import { PlayoffMappingRequest } from '@/modules/division/type/division.d';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import {
  CrossCupConfig,
  CupConfig,
  STAGE_TYPE_LABELS,
  WizardState,
  ZoneConfig,
  getStageBestOf,
  qualifiersToStageTypes,
} from './types';

/**
 * The wizard's single persistence dependency, injected so the build logic
 * below can be unit-tested without mounting React or hitting the network.
 *
 * HU-38: the wizard now persists the WHOLE tournament — base fields plus every
 * division (zone/cross-cup) with its points, cups, playoff mappings and stages
 * — in ONE atomic backend call (`POST /api/tournaments/full`). The graph is
 * created in a single transaction, so it is all-or-nothing: no partial
 * tournament is ever left behind, and there are no per-division/per-stage calls
 * to sequence or partially fail. The backend also creates the tournament
 * already `OpenForRegistration`, so no separate open-registration call is
 * needed.
 *
 * HU-106: the wizard still creates STRUCTURE ONLY. Team registration, stage
 * assignment, and fixture generation are deliberately absent — teams are
 * registered later (registration phase) and the fixture is generated when the
 * tournament's registration closes (HU-38 / HU-107 / HU-108).
 */
export interface WizardServices {
  createFullTournament(
    request: ICreateFullTournamentRequest
  ): Promise<ITournamentResponse | void>;
}

export interface WizardSubmissionResult {
  success: boolean;
  tournamentId?: GUID;
  /** The created tournament's public slug, preferred for navigation when present. */
  slug?: string;
  error?: string;
}

const GROUP_STAGE_DURATION_DAYS = 21;
const ROUND_DURATION_DAYS = 7;

const addDays = (date: Date, days: number): Date => {
  const next = new Date(date);
  next.setDate(next.getDate() + days);
  return next;
};

const roundLabel = (stageType: StageType): string => STAGE_TYPE_LABELS[stageType];

/**
 * HU-112: derives a zone's standings→cup position ranges (HU-45) from its
 * cups' ORDER and qualifier counts, instead of a manual range editor. Cups
 * fill top-down: the first cup takes positions 1..q0, the next q0+1..q0+q1,
 * and so on. A cup without a name is skipped (nothing to seed).
 */
const deriveCupMappings = (cups: CupConfig[]): PlayoffMappingRequest[] => {
  const mappings: PlayoffMappingRequest[] = [];
  let nextPosition = 1;

  for (const cup of cups) {
    const name = cup.name.trim();
    if (name.length === 0 || cup.qualifiers < 1) {
      continue;
    }

    mappings.push({
      fromPosition: nextPosition,
      toPosition: nextPosition + cup.qualifiers - 1,
      destination: name,
    });
    nextPosition += cup.qualifiers;
  }

  return mappings;
};

/**
 * Builds each cup's elimination stages (structure only). HU-112: the rounds
 * are DERIVED from how many teams qualify to the cup, so the bracket always
 * fits its qualifiers (2 → Final; 4 → Semis + Final; 8 → Cuartos + Semis +
 * Final; …). Every round uses the cup's single `bestOf`. Team assignment and
 * match generation are left for later (seeding a knockout needs group-stage
 * standings that do not exist yet). `qualifiersOverride` lets the cross cup
 * derive its rounds from the pooled group total (groups × qualifiersPerGroup).
 */
const buildCupStages = (
  startDate: Date,
  cups: CupConfig[],
  qualifiersOverride?: number
): ICreateFullStageRequest[] => {
  const stages: ICreateFullStageRequest[] = [];

  for (const cup of cups) {
    const qualifiers = qualifiersOverride ?? cup.qualifiers;
    let roundStartDate = startDate;

    for (const stageType of qualifiersToStageTypes(qualifiers)) {
      const roundEndDate = addDays(roundStartDate, ROUND_DURATION_DAYS);

      stages.push({
        name: `${cup.name} - ${roundLabel(stageType)}`,
        stageType,
        isElimination: true,
        startDate: roundStartDate,
        endDate: roundEndDate,
        bracketName: cup.name,
        bestOf: getStageBestOf(cup, stageType),
      });

      roundStartDate = roundEndDate;
    }
  }

  return stages;
};

/**
 * Builds one regular zone (division) as STRUCTURE ONLY (HU-106): its
 * per-division scoring (HU-79), derived standings→cup position ranges (HU-45 /
 * HU-112), an optional group stage, and its playoff cup shells. Returns the
 * nested division payload for the atomic `POST /api/tournaments/full` call.
 */
export const buildZoneDivision = (
  zone: ZoneConfig,
  startDate: Date,
  category: TournamentCategory
): ICreateFullDivisionRequest => {
  const stages: ICreateFullStageRequest[] = [];
  let nextStartDate = startDate;

  if (zone.hasGroupStage) {
    const groupEndDate = addDays(startDate, GROUP_STAGE_DURATION_DAYS);

    // HU-106: the group stage is created as STRUCTURE ONLY. Teams are not
    // registered or assigned here, and no fixture is generated. The backend
    // generates the group-stage fixture later, when the tournament's
    // registration is closed (HU-38 / HU-107 / HU-108), from the teams that
    // registered during the registration phase.
    stages.push({
      name: 'Fase de Grupos',
      stageType: StageType.Group,
      isElimination: false,
      startDate,
      endDate: groupEndDate,
      roundRobinLegs: zone.roundRobinLegs,
    });

    nextStartDate = groupEndDate;
  }

  stages.push(...buildCupStages(nextStartDate, zone.cups));

  return {
    name: zone.name.trim(),
    isCrossDivisionCup: false,
    // Per-division scoring (HU-79). The standings→cup position ranges (HU-45)
    // are DERIVED from the cups' order and qualifier counts (HU-112); the
    // backend uses them to seed each cup from the final group-stage table
    // (HU-81) when the tournament closes.
    pointsForWin: zone.pointsForWin,
    pointsForLoss: zone.pointsForLoss,
    // HU-48: every division MUST carry the tournament's category. The backend
    // rejects a division whose category differs from its tournament, and its
    // Division.Category defaults to Masculine — so a Feminine tournament would
    // have its zones rejected unless we send Feminine explicitly here.
    category,
    playoffMappings: deriveCupMappings(zone.cups),
    stages,
  };
};

/**
 * Builds the cross-division cup (HU-110) as STRUCTURE ONLY: one division
 * flagged `isCrossDivisionCup` carrying `qualifiersPerGroup`, then ONE group
 * stage per configured group ("Grupo 1"…"Grupo N") — every group shares the
 * group-phase window and its own configured RoundRobinLegs — and finally the
 * bracket/cup stages after the groups. Unlike a regular zone (exactly one
 * group), the cross cup fans out into N parallel groups; the backend later
 * pools the top-`qualifiersPerGroup` of every group, sized automatically when
 * the fixture is generated. Teams are not assigned here.
 */
export const buildCrossCupDivision = (
  crossCup: CrossCupConfig,
  startDate: Date,
  category: TournamentCategory
): ICreateFullDivisionRequest => {
  const cupName = crossCup.name.trim();
  const groupEndDate = addDays(startDate, GROUP_STAGE_DURATION_DAYS);
  const stages: ICreateFullStageRequest[] = [];

  // One Group stage per configured group. The backend allows more than one
  // Group stage in a cross-cup division (HU-110). Each runs in the same window;
  // the wizard sends no match count — the bracket auto-sizes later.
  for (let groupNumber = 1; groupNumber <= crossCup.groupCount; groupNumber += 1) {
    stages.push({
      name: `Grupo ${groupNumber}`,
      stageType: StageType.Group,
      isElimination: false,
      startDate,
      endDate: groupEndDate,
      roundRobinLegs: crossCup.roundRobinLegs,
    });
  }

  // HU-112: the cross-cup bracket's rounds are derived from the pooled group
  // total (groups × qualifiers-per-group), not a per-cup qualifier field.
  stages.push(
    ...buildCupStages(
      groupEndDate,
      crossCup.cups,
      crossCup.groupCount * crossCup.qualifiersPerGroup
    )
  );

  return {
    name: cupName,
    isCrossDivisionCup: true,
    // HU-110: how many teams advance per group into the pooled bracket.
    qualifiersPerGroup: crossCup.qualifiersPerGroup,
    pointsForWin: crossCup.pointsForWin,
    pointsForLoss: crossCup.pointsForLoss,
    category,
    // The cross cup pools the top teams of every group into its bracket via the
    // backend seeder (HU-110) — it is NOT seeded from a single division's
    // standings, so it carries no position-range mappings.
    playoffMappings: [],
    stages,
  };
};

/**
 * HU-38: builds ONE {@link ICreateFullTournamentRequest} from the wizard's
 * local state and persists the WHOLE tournament — base fields plus every zone
 * and the optional cross-division cup, each with its points, cups, playoff
 * mappings and stages — in a single atomic backend call. Because the create is
 * transactional, it is all-or-nothing: a single failure fails the whole thing
 * and leaves NO partial tournament behind (so there are no per-step "warnings"
 * to accumulate anymore).
 *
 * HU-106: still STRUCTURE ONLY — no teams are registered and no fixture is
 * generated. The backend creates the tournament already `OpenForRegistration`
 * for the later registration phase.
 */
export const submitWizard = async (
  state: WizardState,
  services: WizardServices
): Promise<WizardSubmissionResult> => {
  const startDate = new Date(state.tournament.startDate);
  const category = state.tournament.category;

  const divisions: ICreateFullDivisionRequest[] = state.zones.map(zone =>
    buildZoneDivision(zone, startDate, category)
  );

  if (state.crossCup.enabled) {
    divisions.push(buildCrossCupDivision(state.crossCup, startDate, category));
  }

  const payload: ICreateFullTournamentRequest = {
    name: state.tournament.name.trim(),
    description: state.tournament.description.trim(),
    startDate,
    teamRegistrationDeadline: new Date(state.tournament.teamRegistrationDeadline),
    // HU-48: the category is set at creation and immutable afterwards.
    category,
    // The season ("Temporada") the tournament belongs to. The wizard always
    // carries the RESOLVED season GUID in state.tournament.seasonId — seeded
    // from the season hub launch (preset, locked select) or chosen manually in
    // the Temporada select — and the /full endpoint persists it (HU-38), so the
    // created tournament is correctly grouped under its season.
    ...(state.tournament.seasonId
      ? { seasonId: state.tournament.seasonId as GUID }
      : {}),
    divisions,
  };

  const tournament = await services.createFullTournament(payload);

  if (!tournament) {
    return { success: false, error: 'No se pudo crear el torneo.' };
  }

  return {
    success: true,
    tournamentId: tournament.id,
    slug: tournament.slug,
  };
};
