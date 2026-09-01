import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import TournamentDivisionAssignment from '@/views/tournament/TournamentDivisionAssignment';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { StageType } from '@/modules/stage/type/stage';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TournamentCategory } from '@/modules/core/enum/tournament/tournamentCategory';
import type { ITeamContextProps, ITeamResponse } from '@/modules/team/type/team.d';
import type { IStageContextProps, IStageResponse } from '@/modules/stage/type/stage';
import type {
  IDivisionContextProps,
  IDivisionResponse,
} from '@/modules/division/type/division';
import type {
  ITournamentCompletability,
  ITournamentContextProps,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/stage/hook/stage.hook');
vi.mock('@/modules/division/hook/division.hook');
vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn() },
}));

import Swal from 'sweetalert2';

const mockedUseTeam = vi.mocked(useTeam);
const mockedUseStage = vi.mocked(useStage);
const mockedUseDivision = vi.mocked(useDivision);
const mockedUseTournament = vi.mocked(useTournament);
const mockedSwalFire = vi.mocked(Swal.fire);

const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

const gid = (value: string): GUID => value as unknown as GUID;

const buildTeam = (id: string, name: string): ITeamResponse => ({
  id: gid(id),
  name,
  slug: name.toLowerCase(),
  threeLetterCode: name.slice(0, 3).toUpperCase(),
  shirtColor: 'Rojo',
  logoUrl: '',
  players: [],
  tournamentId: TOURNAMENT_ID,
});

const buildDivision = (
  id: string,
  name: string,
  isCrossDivisionCup = false
): IDivisionResponse => ({
  id: gid(id),
  name,
  slug: name.toLowerCase().replace(/\s+/g, '-'),
  isFinished: false,
  tournamentId: TOURNAMENT_ID,
  isCrossDivisionCup,
});

const buildGroupStage = (
  id: string,
  divisionId: string,
  name = 'Fase de grupos',
  order = 1
): IStageResponse => ({
  id: gid(id),
  name,
  slug: `grupos-${id}`,
  stageType: StageType.Group,
  isActive: true,
  isElimination: false,
  startDate: '2026-02-01',
  endDate: '2026-03-01',
  divisionId: gid(divisionId),
  order,
  bestOf: 1,
  roundRobinLegs: 1,
});

const buildTournament = (
  status: TournamentStatus
): ITournamentResponse => ({
  id: TOURNAMENT_ID,
  description: 'Torneo de prueba',
  name: 'Apertura',
  slug: 'apertura',
  divisions: [],
  teamRegistrationDeadline: new Date('2026-01-01'),
  startDate: new Date('2026-02-01'),
  status,
  category: TournamentCategory.Masculine,
  seasonId: null,
  seasonName: null,
});

let getTeamsByFiltered: Mock<ITeamContextProps['getTeamsByFiltered']>;
let getDivisionsByFilters: Mock<IDivisionContextProps['getDivisionsByFilters']>;
let getStagesByFilters: Mock<IStageContextProps['getStagesByFilters']>;
let assignTeamsToStage: Mock<IStageContextProps['assignTeamsToStage']>;
let unassignTeamsFromStage: Mock<IStageContextProps['unassignTeamsFromStage']>;
let getCompletability: Mock<ITournamentContextProps['getCompletability']>;
let putTournamentById: Mock<ITournamentContextProps['putTournamentById']>;

const page = <T,>(totalCount: number, items: T[]) => ({
  items,
  page: 1,
  pageSize: 300,
  totalCount,
});

const setup = (options: {
  status?: TournamentStatus;
  divisions?: IDivisionResponse[];
  groupStageByDivision?: Record<string, IStageResponse>;
  groupStagesByDivision?: Record<string, IStageResponse[]>;
  enrolled?: ITeamResponse[];
  assignedByStage?: Record<string, ITeamResponse[]>;
  completability?: ITournamentCompletability;
}) => {
  const status = options.status ?? TournamentStatus.RegistrationClosed;
  const divisions = options.divisions ?? [];
  const groupStageByDivision = options.groupStageByDivision ?? {};
  const groupStagesByDivision = options.groupStagesByDivision ?? {};
  const enrolled = options.enrolled ?? [];
  const assignedByStage = options.assignedByStage ?? {};
  const completability =
    options.completability ?? { canStart: true, issues: [] };

  getTeamsByFiltered = vi.fn<ITeamContextProps['getTeamsByFiltered']>();
  getTeamsByFiltered.mockImplementation(async filter => {
    if (filter?.stageId) {
      const assigned = assignedByStage[filter.stageId as string] ?? [];
      return page(assigned.length, assigned);
    }
    return page(enrolled.length, enrolled);
  });

  getDivisionsByFilters = vi.fn<IDivisionContextProps['getDivisionsByFilters']>();
  getDivisionsByFilters.mockResolvedValue(page(divisions.length, divisions));

  getStagesByFilters = vi.fn<IStageContextProps['getStagesByFilters']>();
  getStagesByFilters.mockImplementation(async filter => {
    const divisionId = filter?.divisionId as string;
    const single = groupStageByDivision[divisionId];
    const stages =
      groupStagesByDivision[divisionId] ?? (single ? [single] : []);
    return page(stages.length, stages);
  });

  assignTeamsToStage = vi.fn<IStageContextProps['assignTeamsToStage']>();
  assignTeamsToStage.mockResolvedValue(true);

  unassignTeamsFromStage = vi.fn<IStageContextProps['unassignTeamsFromStage']>();
  unassignTeamsFromStage.mockResolvedValue(true);

  getCompletability = vi.fn<ITournamentContextProps['getCompletability']>();
  getCompletability.mockResolvedValue(completability);

  putTournamentById = vi.fn<ITournamentContextProps['putTournamentById']>();
  putTournamentById.mockResolvedValue(true);

  mockedUseTeam.mockReturnValue({
    team: null,
    teams: null,
    addTeam: vi.fn(),
    putTeamById: vi.fn(),
    putTeamLogoById: vi.fn(),
    getTeamsByFiltered,
    getTeamById: vi.fn(),
    deleteTeamById: vi.fn(),
  } as ITeamContextProps);

  mockedUseStage.mockReturnValue({
    stage: null,
    stages: null,
    addStage: vi.fn(),
    putStageById: vi.fn(),
    getStagesByFilters,
    getStageById: vi.fn(),
    deleteStagesById: vi.fn(),
    generateStagesAutomatically: vi.fn(),
    assignTeamsToStage,
    unassignTeamsFromStage,
    seedKnockoutStage: vi.fn(),
  } as IStageContextProps);

  mockedUseDivision.mockReturnValue({
    division: null,
    divisions: null,
    addDivision: vi.fn(),
    generateFixtureByDivisionId: vi.fn(),
    putDivisionById: vi.fn(),
    getDivisionsByFilters,
    getDivisionsById: vi.fn(),
    deleteDivisionsById: vi.fn(),
  } as IDivisionContextProps);

  mockedUseTournament.mockReturnValue({
    tournament: null,
    tournaments: null,
    addTournament: vi.fn(),
    createFullTournament: vi.fn(),
    addFullDivision: vi.fn(),
    getAllTournamentsByFilter: vi.fn(),
    getTournamentById: vi.fn(),
    putTournamentById,
    deleteTournamentById: vi.fn(),
    registerTeamsByTournamentId: vi.fn(),
    enrollTeam: vi.fn(),
    unenrollTeam: vi.fn(),
    getCompletability,
  } as ITournamentContextProps);

  return { tournament: buildTournament(status) };
};

const renderComponent = (tournament: ITournamentResponse) =>
  render(<TournamentDivisionAssignment tournament={tournament} />);

/** Opens the "Agregar equipos" picker for a zone region and returns the dialog. */
const openPicker = async (
  user: ReturnType<typeof userEvent.setup>,
  region: HTMLElement
) => {
  await user.click(
    within(region).getByRole('button', { name: /agregar equipos/i })
  );
  return screen.findByRole('dialog');
};

beforeEach(() => {
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('TournamentDivisionAssignment — draft availability', () => {
  it('is available as a draft while registration is still open', async () => {
    const { tournament } = setup({
      status: TournamentStatus.OpenForRegistration,
      divisions: [buildDivision('div-a', 'Zona A')],
      groupStageByDivision: { 'div-a': buildGroupStage('gs-a', 'div-a') },
      enrolled: [buildTeam('river-id', 'River')],
      assignedByStage: { 'gs-a': [] },
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    expect(
      await screen.findByText(/asignando los equipos a sus zonas como borrador/i)
    ).toBeInTheDocument();
    expect(getDivisionsByFilters).toHaveBeenCalled();
  });

  it('is unavailable once the tournament has started', async () => {
    const { tournament } = setup({ status: TournamentStatus.Ongoing });

    renderComponent(tournament);

    expect(
      await screen.findByText(/no está disponible una vez que el torneo comenzó/i)
    ).toBeInTheDocument();
    expect(getDivisionsByFilters).not.toHaveBeenCalled();
  });
});

describe('TournamentDivisionAssignment — assigning teams', () => {
  it('assigns a picked team to its zone group stage', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      groupStageByDivision: { 'div-a': groupA },
      enrolled: [river, boca],
      assignedByStage: { 'gs-a': [] },
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    const dialog = await openPicker(user, region);

    await user.click(within(dialog).getByText('River'));
    await user.click(within(dialog).getByRole('button', { name: /agregar \(1\)/i }));

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(groupA.id, [river.id]);
  });

  it('removes (unassigns) a team from its zone', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      groupStageByDivision: { 'div-a': groupA },
      enrolled: [river],
      assignedByStage: { 'gs-a': [river] },
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(within(region).getByRole('button', { name: /quitar River/i }));

    await waitFor(() => expect(unassignTeamsFromStage).toHaveBeenCalledTimes(1));
    expect(unassignTeamsFromStage).toHaveBeenCalledWith(groupA.id, [river.id]);
  });

  it('excludes a team already in a regular zone from another zone but not from the cross cup', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const zoneA = buildDivision('div-a', 'Zona A');
    const zoneB = buildDivision('div-b', 'Zona B');
    const crossCup = buildDivision('div-c', 'Copa Club12', true);

    const { tournament } = setup({
      divisions: [zoneA, zoneB, crossCup],
      groupStageByDivision: {
        'div-a': buildGroupStage('gs-a', 'div-a'),
        'div-b': buildGroupStage('gs-b', 'div-b'),
        'div-c': buildGroupStage('gs-c', 'div-c'),
      },
      enrolled: [river, boca],
      assignedByStage: { 'gs-a': [river], 'gs-b': [], 'gs-c': [] },
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    // River is in Zona A → not offered in Zona B's picker; Boca is.
    const zoneBRegion = await screen.findByRole('region', { name: /Zona B/i });
    const zoneBDialog = await openPicker(user, zoneBRegion);
    expect(within(zoneBDialog).queryByText('River')).not.toBeInTheDocument();
    expect(within(zoneBDialog).getByText('Boca')).toBeInTheDocument();
    await user.click(within(zoneBDialog).getByRole('button', { name: /cancelar/i }));

    // The cross cup is a parallel membership → River is still offered there.
    const crossRegion = await screen.findByRole('region', { name: /Copa Club12/i });
    const crossDialog = await openPicker(user, crossRegion);
    expect(within(crossDialog).getByText('River')).toBeInTheDocument();
  });
});

describe('TournamentDivisionAssignment — multi-group cross cup (HU-110)', () => {
  it('assigns a team to the chosen cross-cup group', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const crossCup = buildDivision('div-c', 'Copa Club12', true);
    const grupo1 = buildGroupStage('gs-c1', 'div-c', 'Grupo 1', 1);
    const grupo2 = buildGroupStage('gs-c2', 'div-c', 'Grupo 2', 2);

    const { tournament } = setup({
      divisions: [crossCup],
      groupStagesByDivision: { 'div-c': [grupo1, grupo2] },
      enrolled: [river, boca],
      assignedByStage: { 'gs-c1': [], 'gs-c2': [] },
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const grupo2Region = await screen.findByRole('region', { name: /^Grupo 2$/i });
    const dialog = await openPicker(user, grupo2Region);
    await user.click(within(dialog).getByText('River'));
    await user.click(within(dialog).getByRole('button', { name: /agregar \(1\)/i }));

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(grupo2.id, [river.id]);
  });
});

describe('TournamentDivisionAssignment — completability panel', () => {
  it('renders a Spanish message for each issue code', async () => {
    const { tournament } = setup({
      divisions: [],
      completability: {
        canStart: false,
        issues: [
          { code: 'ZoneTooFewTeams', divisionName: 'Zona A', assignedTeams: 1 },
          { code: 'TeamNotAssigned', teamName: 'River' },
          { code: 'TeamInMultipleZones', teamName: 'Boca' },
          {
            code: 'PlayoffRangeExceedsTeams',
            divisionName: 'Zona B',
            fromPosition: 5,
            assignedTeams: 3,
          },
          { code: 'CrossCupGroupTooFewTeams', assignedTeams: 1 },
        ],
      },
    });

    renderComponent(tournament);

    expect(
      await screen.findByText('La zona Zona A tiene 1 equipos (mínimo 2).')
    ).toBeInTheDocument();
    expect(
      screen.getByText('River está inscripto pero sin zona asignada.')
    ).toBeInTheDocument();
    expect(
      screen.getByText('Boca está asignado a más de una zona.')
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'En Zona B, un rango de playoff arranca en la posición 5 pero solo hay 3 equipos.'
      )
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        'Un grupo de la copa cruzada tiene 1 equipos (mínimo 2).'
      )
    ).toBeInTheDocument();
  });
});

describe('TournamentDivisionAssignment — start button', () => {
  it('disables "Iniciar torneo" while completability says it cannot start', async () => {
    const { tournament } = setup({
      divisions: [],
      completability: {
        canStart: false,
        issues: [{ code: 'TeamNotAssigned', teamName: 'River' }],
      },
    });

    renderComponent(tournament);

    const startButton = await screen.findByRole('button', {
      name: /iniciar torneo/i,
    });
    expect(startButton).toBeDisabled();
  });

  it('enables "Iniciar torneo" and starts the tournament when completability allows it', async () => {
    const { tournament } = setup({
      divisions: [],
      completability: { canStart: true, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const startButton = await screen.findByRole('button', {
      name: /iniciar torneo/i,
    });
    await waitFor(() => expect(startButton).toBeEnabled());

    await user.click(startButton);

    await waitFor(() => expect(putTournamentById).toHaveBeenCalledTimes(1));
    expect(putTournamentById).toHaveBeenCalledWith(
      TOURNAMENT_ID,
      expect.objectContaining({ status: TournamentStatus.Ongoing })
    );
  });

  it('closes registration then starts when launched from an open-registration draft', async () => {
    const { tournament } = setup({
      status: TournamentStatus.OpenForRegistration,
      divisions: [],
      completability: { canStart: true, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const startButton = await screen.findByRole('button', {
      name: /iniciar torneo/i,
    });
    await waitFor(() => expect(startButton).toBeEnabled());
    await user.click(startButton);

    await waitFor(() => expect(putTournamentById).toHaveBeenCalledTimes(2));
    expect(putTournamentById).toHaveBeenNthCalledWith(
      1,
      TOURNAMENT_ID,
      expect.objectContaining({ status: TournamentStatus.RegistrationClosed })
    );
    expect(putTournamentById).toHaveBeenNthCalledWith(
      2,
      TOURNAMENT_ID,
      expect.objectContaining({ status: TournamentStatus.Ongoing })
    );
  });
});
