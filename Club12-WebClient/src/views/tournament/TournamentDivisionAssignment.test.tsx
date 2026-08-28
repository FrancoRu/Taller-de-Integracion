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
});

let getTeamsByFiltered: Mock<ITeamContextProps['getTeamsByFiltered']>;
let getDivisionsByFilters: Mock<IDivisionContextProps['getDivisionsByFilters']>;
let getStagesByFilters: Mock<IStageContextProps['getStagesByFilters']>;
let assignTeamsToStage: Mock<IStageContextProps['assignTeamsToStage']>;
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
  // HU-110: a cross-division cup has more than one Group stage, so a division
  // can map to a LIST of stages. Merged with the singular map above.
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

  getCompletability = vi.fn<ITournamentContextProps['getCompletability']>();
  getCompletability.mockResolvedValue(completability);

  putTournamentById = vi.fn<ITournamentContextProps['putTournamentById']>();
  putTournamentById.mockResolvedValue(undefined);

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

describe('TournamentDivisionAssignment — gating', () => {
  it('does not render the assignment UI unless the tournament is RegistrationClosed', async () => {
    const { tournament } = setup({
      status: TournamentStatus.OpenForRegistration,
    });

    renderComponent(tournament);

    expect(
      await screen.findByText(/la asignación estará disponible/i)
    ).toBeInTheDocument();
    expect(getDivisionsByFilters).not.toHaveBeenCalled();
  });
});

describe('TournamentDivisionAssignment — assigning teams', () => {
  it('assigns an enrolled team to its zone group stage', async () => {
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
    await user.click(within(region).getByRole('button', { name: /agregar River/i }));

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(groupA.id, [river.id]);
  });

  it('excludes a team already in a regular zone from other zones but not from the cross cup', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const zoneA = buildDivision('div-a', 'Zona A');
    const zoneB = buildDivision('div-b', 'Zona B');
    const crossCup = buildDivision('div-c', 'Copa Club12', true);
    const groupA = buildGroupStage('gs-a', 'div-a');
    const groupB = buildGroupStage('gs-b', 'div-b');
    const groupC = buildGroupStage('gs-c', 'div-c');

    const { tournament } = setup({
      divisions: [zoneA, zoneB, crossCup],
      groupStageByDivision: {
        'div-a': groupA,
        'div-b': groupB,
        'div-c': groupC,
      },
      enrolled: [river, boca],
      assignedByStage: {
        'gs-a': [river],
        'gs-b': [],
        'gs-c': [],
      },
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    const zoneBRegion = await screen.findByRole('region', { name: /Zona B/i });
    expect(
      within(zoneBRegion).queryByRole('button', { name: /agregar River/i })
    ).not.toBeInTheDocument();
    expect(
      within(zoneBRegion).getByRole('button', { name: /agregar Boca/i })
    ).toBeInTheDocument();

    const crossRegion = await screen.findByRole('region', {
      name: /Copa Club12/i,
    });
    expect(
      within(crossRegion).getByRole('button', { name: /agregar River/i })
    ).toBeInTheDocument();
  });
});

describe('TournamentDivisionAssignment — multi-group cross cup (HU-110)', () => {
  it('shows every cross-cup group and assigns a team to the chosen group', async () => {
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

    // Both groups are rendered as their own labelled sections.
    const grupo1Region = await screen.findByRole('region', { name: /^Grupo 1$/i });
    const grupo2Region = await screen.findByRole('region', { name: /^Grupo 2$/i });
    expect(grupo1Region).toBeInTheDocument();
    expect(grupo2Region).toBeInTheDocument();

    // Assigning River to Grupo 2 targets that group's stage, not Grupo 1's.
    await user.click(
      within(grupo2Region).getByRole('button', { name: /agregar River/i })
    );

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(grupo2.id, [river.id]);
  });

  it('excludes a team already in one cross-cup group from the other groups of the same cup', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const crossCup = buildDivision('div-c', 'Copa Club12', true);
    const grupo1 = buildGroupStage('gs-c1', 'div-c', 'Grupo 1', 1);
    const grupo2 = buildGroupStage('gs-c2', 'div-c', 'Grupo 2', 2);

    const { tournament } = setup({
      divisions: [crossCup],
      groupStagesByDivision: { 'div-c': [grupo1, grupo2] },
      enrolled: [river, boca],
      assignedByStage: { 'gs-c1': [river], 'gs-c2': [] },
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    // River already plays Grupo 1, so it must not be offered in Grupo 2; Boca
    // (in neither) still can be added there.
    const grupo2Region = await screen.findByRole('region', { name: /^Grupo 2$/i });
    expect(
      within(grupo2Region).queryByRole('button', { name: /agregar River/i })
    ).not.toBeInTheDocument();
    expect(
      within(grupo2Region).getByRole('button', { name: /agregar Boca/i })
    ).toBeInTheDocument();
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
});
