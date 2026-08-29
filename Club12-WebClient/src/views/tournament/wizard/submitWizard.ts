import { GUID } from '@/modules/core/types/types';
import { IAddTournamentRequest, ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import { AddDivisionRequest, IDivisionResponse } from '@/modules/division/type/division';
import { IAddStageRequest, IStageResponse, StageType } from '@/modules/stage/type/stage';
import { PlayoffMappingRequest } from '@/modules/division/type/division.d';
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
 * The wizard's persistence dependencies, injected so the orchestration
 * logic below can be unit-tested without mounting React or hitting the
 * network — every wizard-context hook exposes a function matching one of
 * these signatures already.
 *
 * HU-106: the wizard creates STRUCTURE ONLY (tournament + divisions +
 * stages). Team registration, stage assignment, and fixture generation are
 * deliberately absent — teams are registered later (registration phase) and
 * the fixture is generated when the tournament's registration closes
 * (HU-38 / HU-107 / HU-108).
 */
export interface WizardServices {
  addTournament(request: IAddTournamentRequest): Promise<ITournamentResponse | void>;
  /**
   * Moves the freshly-created tournament from its initial `Scheduled` status
   * to `OpenForRegistration`. The backend freezes structural edits unless the
   * tournament is `OpenForRegistration` (HU-31), so this MUST run before any
   * division/stage is created — otherwise every `addDivision` is rejected with
   * 409. Leaves the tournament open for the registration phase (HU-106).
   */
  openRegistration(tournamentId: GUID): Promise<boolean | void>;
  addDivision(request: AddDivisionRequest): Promise<IDivisionResponse | void>;
  addStage(request: IAddStageRequest): Promise<IStageResponse | void>;
}

export interface WizardSubmissionResult {
  success: boolean;
  tournamentId?: GUID;
  error?: string;
  warnings: string[];
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
 * Creates each cup's elimination stages (structure only). HU-112: the rounds
 * are DERIVED from how many teams qualify to the cup, so the bracket always
 * fits its qualifiers (2 → Final; 4 → Semis + Final; 8 → Cuartos + Semis +
 * Final; …). Every round uses the cup's single `bestOf`. Team assignment and
 * match generation are left for later (seeding a knockout needs group-stage
 * standings that do not exist yet). `qualifiersOverride` lets the cross cup
 * derive its rounds from the pooled group total (groups × qualifiersPerGroup).
 */
const createCupStages = async (
  services: WizardServices,
  divisionId: GUID,
  startDate: Date,
  cups: CupConfig[],
  warnings: string[],
  qualifiersOverride?: number
): Promise<void> => {
  for (const cup of cups) {
    const qualifiers = qualifiersOverride ?? cup.qualifiers;
    let roundStartDate = startDate;

    for (const stageType of qualifiersToStageTypes(qualifiers)) {
      const roundEndDate = addDays(roundStartDate, ROUND_DURATION_DAYS);

      const stage = await services.addStage({
        name: `${cup.name} - ${roundLabel(stageType)}`,
        stageType,
        isElimination: true,
        startDate: roundStartDate,
        endDate: roundEndDate,
        divisionId,
        bracketName: cup.name,
        bestOf: getStageBestOf(cup, stageType),
      });

      if (!stage) {
        warnings.push(`No se pudo crear la ronda "${roundLabel(stageType)}" de "${cup.name}".`);
      }

      roundStartDate = roundEndDate;
    }
  }
};

const createZoneStructure = async (
  services: WizardServices,
  tournamentId: GUID,
  zoneName: string,
  hasGroupStage: boolean,
  roundRobinLegs: number,
  cups: CupConfig[],
  startDate: Date,
  isCrossDivisionCup: boolean,
  pointsForWin: number,
  pointsForLoss: number,
  category: TournamentCategory,
  warnings: string[]
): Promise<IDivisionResponse | null> => {
  const division = await services.addDivision({
    name: zoneName,
    tournamentId,
    isCrossDivisionCup,
    // Per-division scoring (HU-79). The standings→cup position ranges (HU-45)
    // are DERIVED from the cups' order and qualifier counts (HU-112); the
    // backend uses them to seed each cup from the final group-stage table
    // (HU-81) when the tournament closes.
    pointsForWin,
    pointsForLoss,
    // HU-48: every division MUST carry the tournament's category. The backend
    // rejects a division whose category differs from its tournament, and its
    // Division.Category defaults to Masculine — so a Feminine tournament would
    // have its zones rejected unless we send Feminine explicitly here.
    category,
    playoffMappings: deriveCupMappings(cups),
  });

  if (!division) {
    warnings.push(`No se pudo crear la zona "${zoneName}".`);
    return null;
  }

  let nextStartDate = startDate;

  if (hasGroupStage) {
    const groupEndDate = addDays(startDate, GROUP_STAGE_DURATION_DAYS);

    // HU-106: the group stage is created as STRUCTURE ONLY. Teams are not
    // registered or assigned here, and no fixture is generated. The backend
    // generates the group-stage fixture later, when the tournament's
    // registration is closed (HU-38 / HU-107 / HU-108), from the teams that
    // registered during the registration phase.
    const groupStage = await services.addStage({
      name: 'Fase de Grupos',
      stageType: StageType.Group,
      isElimination: false,
      startDate,
      endDate: groupEndDate,
      divisionId: division.id,
      roundRobinLegs,
    });

    if (!groupStage) {
      warnings.push(`No se pudo crear la fase de grupos de "${zoneName}".`);
    }

    nextStartDate = groupEndDate;
  }

  await createCupStages(services, division.id, nextStartDate, cups, warnings);

  return division;
};

/**
 * Materializes the cross-division cup (HU-110) as STRUCTURE ONLY: one
 * division flagged `isCrossDivisionCup` carrying `qualifiersPerGroup`, then
 * ONE group stage per configured group ("Grupo 1"…"Grupo N") — every group
 * shares the group-phase window and its own configured RoundRobinLegs — and
 * finally the bracket/cup stages after the groups. Unlike a regular zone
 * (exactly one group), the cross cup fans out into N parallel groups; the
 * backend later pools the top-`qualifiersPerGroup` of every group, sized
 * automatically when the fixture is generated. Teams are not assigned here.
 */
const createCrossCupStructure = async (
  services: WizardServices,
  tournamentId: GUID,
  crossCup: CrossCupConfig,
  startDate: Date,
  category: TournamentCategory,
  warnings: string[]
): Promise<void> => {
  const cupName = crossCup.name.trim();

  const division = await services.addDivision({
    name: cupName,
    tournamentId,
    isCrossDivisionCup: true,
    // HU-110: how many teams advance per group into the pooled bracket.
    qualifiersPerGroup: crossCup.qualifiersPerGroup,
    pointsForWin: crossCup.pointsForWin,
    pointsForLoss: crossCup.pointsForLoss,
    category,
    // The cross cup pools the top teams of every group into its bracket via
    // the backend seeder (HU-110) — it is NOT seeded from a single division's
    // standings, so it carries no position-range mappings.
    playoffMappings: [],
  });

  if (!division) {
    warnings.push(`No se pudo crear la copa cruzada "${cupName}".`);
    return;
  }

  const groupEndDate = addDays(startDate, GROUP_STAGE_DURATION_DAYS);

  // One Group stage per configured group. The backend now allows more than
  // one Group stage in a cross-cup division (HU-110). Each runs in the same
  // window; the wizard sends no match count — the bracket auto-sizes later.
  for (let groupNumber = 1; groupNumber <= crossCup.groupCount; groupNumber += 1) {
    const groupStage = await services.addStage({
      name: `Grupo ${groupNumber}`,
      stageType: StageType.Group,
      isElimination: false,
      startDate,
      endDate: groupEndDate,
      divisionId: division.id,
      roundRobinLegs: crossCup.roundRobinLegs,
    });

    if (!groupStage) {
      warnings.push(`No se pudo crear el grupo ${groupNumber} de "${cupName}".`);
    }
  }

  // HU-112: the cross-cup bracket's rounds are derived from the pooled group
  // total (groups × qualifiers-per-group), not a per-cup qualifier field.
  await createCupStages(
    services,
    division.id,
    groupEndDate,
    crossCup.cups,
    warnings,
    crossCup.groupCount * crossCup.qualifiersPerGroup
  );
};

/**
 * Sequences every API call needed to materialize a wizard's local state as
 * STRUCTURE ONLY (HU-106): the tournament, each zone (division + optional
 * group stage + playoff cup shells), and the optional cross-division cup.
 * No teams are registered and no fixture is generated — the tournament is
 * left in OpenForRegistration for the later registration phase. Nothing is
 * transactional — if a step fails, prior steps are NOT rolled back (that
 * state is real and left for the admin to fix from the normal panel), and
 * the failure is surfaced as a warning rather than aborting the whole run,
 * except for the tournament itself: without it nothing else can proceed.
 */
export const submitWizard = async (
  state: WizardState,
  services: WizardServices
): Promise<WizardSubmissionResult> => {
  const warnings: string[] = [];

  const tournament = await services.addTournament({
    name: state.tournament.name.trim(),
    description: state.tournament.description.trim(),
    startDate: new Date(state.tournament.startDate),
    teamRegistrationDeadline: new Date(state.tournament.teamRegistrationDeadline),
    // HU-48: the category is set at creation and immutable afterwards.
    category: state.tournament.category,
    // Optional grouping into a season ("Temporada"). Only sent when chosen so
    // an unset select leaves the tournament without a season.
    ...(state.tournament.seasonId
      ? { seasonId: state.tournament.seasonId as GUID }
      : {}),
  });

  if (!tournament) {
    return { success: false, error: 'No se pudo crear el torneo.', warnings };
  }

  // Open registration BEFORE building the structure: the backend rejects any
  // division/stage creation unless the tournament is OpenForRegistration
  // (HU-31), and a new tournament starts as Scheduled. Without this, every
  // zone fails with 409. If it fails the structure can't be built, so abort.
  const opened = await services.openRegistration(tournament.id);
  if (!opened) {
    return {
      success: false,
      tournamentId: tournament.id,
      error:
        'El torneo se creó pero no se pudo abrir la inscripción, así que no se pudieron crear las zonas. Abrí la inscripción desde el panel y agregá las divisiones.',
      warnings,
    };
  }

  const startDate = new Date(state.tournament.startDate);

  for (const zone of state.zones as ZoneConfig[]) {
    await createZoneStructure(
      services,
      tournament.id,
      zone.name.trim(),
      zone.hasGroupStage,
      zone.roundRobinLegs,
      zone.cups,
      startDate,
      false,
      zone.pointsForWin,
      zone.pointsForLoss,
      state.tournament.category,
      warnings
    );
  }

  if (state.crossCup.enabled) {
    await createCrossCupStructure(
      services,
      tournament.id,
      state.crossCup,
      startDate,
      state.tournament.category,
      warnings
    );
  }

  return { success: true, tournamentId: tournament.id, warnings };
};
