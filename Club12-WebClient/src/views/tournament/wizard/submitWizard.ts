import { GUID } from '@/modules/core/types/types';
import { IAddTournamentRequest, ITournamentResponse } from '@/modules/tournament/type/tournament.d';
import { AddDivisionRequest, IDivisionResponse } from '@/modules/division/type/division';
import { IAddStageRequest, IStageResponse, StageType } from '@/modules/stage/type/stage.d';
import { CupConfig, STAGE_TYPE_LABELS, WizardState, ZoneConfig } from './types';
import { resolveCrossCupTeamIds } from './wizardLogic';

/**
 * The wizard's persistence dependencies, injected so the orchestration
 * logic below can be unit-tested without mounting React or hitting the
 * network — every wizard-context hook exposes a function matching one of
 * these signatures already.
 */
export interface WizardServices {
  addTournament(request: IAddTournamentRequest): Promise<ITournamentResponse | void>;
  registerTeams(tournamentId: GUID, teamIds: GUID[]): Promise<boolean | void>;
  addDivision(request: AddDivisionRequest): Promise<IDivisionResponse | void>;
  addStage(request: IAddStageRequest): Promise<IStageResponse | void>;
  assignTeamsToStage(stageId: GUID, teamIds: GUID[], auto?: boolean): Promise<boolean | void>;
  generateMatches(stageId: GUID): Promise<boolean>;
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
 * Creates the cup's elimination stages (structure only — bracket name and
 * best-of per round). Team assignment and match generation are
 * deliberately left for later: seeding a knockout round needs group-stage
 * standings that do not exist yet at tournament-creation time.
 */
const createCupStages = async (
  services: WizardServices,
  divisionId: GUID,
  startDate: Date,
  cups: CupConfig[],
  warnings: string[]
): Promise<void> => {
  for (const cup of cups) {
    let roundStartDate = startDate;

    for (const round of cup.rounds) {
      const roundEndDate = addDays(roundStartDate, ROUND_DURATION_DAYS);

      const stage = await services.addStage({
        name: `${cup.name} - ${roundLabel(round.stageType)}`,
        stageType: round.stageType,
        isElimination: true,
        startDate: roundStartDate,
        endDate: roundEndDate,
        divisionId,
        bracketName: cup.name,
        bestOf: round.bestOf,
      });

      if (!stage) {
        warnings.push(`No se pudo crear la ronda "${roundLabel(round.stageType)}" de "${cup.name}".`);
      }

      roundStartDate = roundEndDate;
    }
  }
};

const createZoneStructure = async (
  services: WizardServices,
  tournamentId: GUID,
  zoneName: string,
  teamIds: GUID[],
  hasGroupStage: boolean,
  roundRobinLegs: number,
  cups: CupConfig[],
  startDate: Date,
  isCrossDivisionCup: boolean,
  warnings: string[]
): Promise<IDivisionResponse | null> => {
  const division = await services.addDivision({
    name: zoneName,
    tournamentId,
    isCrossDivisionCup,
  });

  if (!division) {
    warnings.push(`No se pudo crear la zona "${zoneName}".`);
    return null;
  }

  let nextStartDate = startDate;

  if (hasGroupStage) {
    const groupEndDate = addDays(startDate, GROUP_STAGE_DURATION_DAYS);

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
    } else {
      const assigned = await services.assignTeamsToStage(groupStage.id, teamIds, false);
      if (!assigned) {
        warnings.push(`No se pudieron asignar los equipos a la fase de grupos de "${zoneName}".`);
      } else {
        const generated = await services.generateMatches(groupStage.id);
        if (!generated) {
          warnings.push(`No se pudo generar el fixture de la fase de grupos de "${zoneName}".`);
        }
      }
    }

    nextStartDate = groupEndDate;
  }

  await createCupStages(services, division.id, nextStartDate, cups, warnings);

  return division;
};

/**
 * Sequences every API call needed to materialize a wizard's local state:
 * the tournament, its registered teams, each zone (division + optional
 * group stage + playoff cup shells), and the optional cross-division cup.
 * Nothing is transactional — if a step fails, prior steps are NOT rolled
 * back (that state is real and left for the admin to fix from the normal
 * panel), and the failure is surfaced as a warning rather than aborting
 * the whole run, except for the tournament itself: without it nothing
 * else can proceed.
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
    minTeams: state.tournament.minTeams,
    maxTeams: state.tournament.maxTeams,
  });

  if (!tournament) {
    return { success: false, error: 'No se pudo crear el torneo.', warnings };
  }

  if (state.selectedTeamIds.length > 0) {
    const registered = await services.registerTeams(tournament.id, state.selectedTeamIds);
    if (!registered) {
      warnings.push('No se pudieron registrar los equipos en el torneo.');
    }
  }

  const startDate = new Date(state.tournament.startDate);

  for (const zone of state.zones as ZoneConfig[]) {
    await createZoneStructure(
      services,
      tournament.id,
      zone.name.trim(),
      zone.teamIds,
      zone.hasGroupStage,
      zone.roundRobinLegs,
      zone.cups,
      startDate,
      false,
      warnings
    );
  }

  if (state.crossCup.enabled) {
    await createZoneStructure(
      services,
      tournament.id,
      state.crossCup.name.trim(),
      resolveCrossCupTeamIds(state),
      state.crossCup.hasGroupStage,
      state.crossCup.roundRobinLegs,
      state.crossCup.cups,
      startDate,
      true,
      warnings
    );
  }

  return { success: true, tournamentId: tournament.id, warnings };
};
