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
  default: { fire: vi.fn(), getContainer: vi.fn().mockReturnValue(null) },
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
  tournamentName: null,
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

const buildEliminationStage = (
  id: string,
  divisionId: string,
  name = 'Final',
  order = 1,
  drawnAt: string | null = null
): IStageResponse => ({
  id: gid(id),
  name,
  slug: `final-${id}`,
  stageType: StageType.Final,
  isActive: true,
  isElimination: true,
  startDate: '2026-02-01',
  endDate: '2026-03-01',
  divisionId: gid(divisionId),
  order,
  bestOf: 1,
  roundRobinLegs: 1,
  drawnAt,
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
let getRoster: Mock<IDivisionContextProps['getRoster']>;
let enrollTeams: Mock<IDivisionContextProps['enrollTeams']>;
let unenrollTeams: Mock<IDivisionContextProps['unenrollTeams']>;
let autoDistribute: Mock<IDivisionContextProps['autoDistribute']>;
let rebuildSubGroups: Mock<IDivisionContextProps['rebuildSubGroups']>;
let reassignTeamToSubGroup: Mock<IDivisionContextProps['reassignTeamToSubGroup']>;
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
  /** divisionId -> that division's roster (DivisionTeamRegistration teams). */
  rosterByDivision?: Record<string, ITeamResponse[]>;
  /** divisionId -> its Group-type stages (sub-groups). */
  groupStagesByDivision?: Record<string, IStageResponse[]>;
  /** divisionId -> its elimination stages (bracket). */
  eliminationStagesByDivision?: Record<string, IStageResponse[]>;
  /** whole-tournament enrolled teams (the pool a division can enrol from). */
  enrolled?: ITeamResponse[];
  /** stageId -> teams already placed in that stage (group or bracket). */
  assignedByStage?: Record<string, ITeamResponse[]>;
  completability?: ITournamentCompletability;
}) => {
  const status = options.status ?? TournamentStatus.RegistrationClosed;
  const divisions = options.divisions ?? [];
  const rosterByDivision = options.rosterByDivision ?? {};
  const groupStagesByDivision = options.groupStagesByDivision ?? {};
  const eliminationStagesByDivision = options.eliminationStagesByDivision ?? {};
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
    const groups = groupStagesByDivision[divisionId] ?? [];
    const eliminations = eliminationStagesByDivision[divisionId] ?? [];
    const stages = [...groups, ...eliminations];
    return page(stages.length, stages);
  });

  assignTeamsToStage = vi.fn<IStageContextProps['assignTeamsToStage']>();
  assignTeamsToStage.mockResolvedValue(true);

  unassignTeamsFromStage = vi.fn<IStageContextProps['unassignTeamsFromStage']>();
  unassignTeamsFromStage.mockResolvedValue(true);

  getRoster = vi.fn<IDivisionContextProps['getRoster']>();
  getRoster.mockImplementation(async divisionId =>
    rosterByDivision[divisionId as string] ?? []
  );

  enrollTeams = vi.fn<IDivisionContextProps['enrollTeams']>();
  enrollTeams.mockResolvedValue(true);

  unenrollTeams = vi.fn<IDivisionContextProps['unenrollTeams']>();
  unenrollTeams.mockResolvedValue(true);

  autoDistribute = vi.fn<IDivisionContextProps['autoDistribute']>();
  autoDistribute.mockResolvedValue(true);

  rebuildSubGroups = vi.fn<IDivisionContextProps['rebuildSubGroups']>();
  rebuildSubGroups.mockResolvedValue(true);

  reassignTeamToSubGroup = vi.fn<IDivisionContextProps['reassignTeamToSubGroup']>();
  reassignTeamToSubGroup.mockResolvedValue(true);

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
    assignTeamsToStage,
    unassignTeamsFromStage,
    seedKnockoutStage: vi.fn(),
    previewDraw: vi.fn(),
    commitDraw: vi.fn(),
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
    getRoster,
    enrollTeams,
    unenrollTeams,
    autoDistribute,
    rebuildSubGroups,
    reassignTeamToSubGroup,
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
    enrollTeam: vi.fn(),
    unenrollTeam: vi.fn(),
    getCompletability,
    getStructure: vi.fn(),
  } as ITournamentContextProps);

  return { tournament: buildTournament(status) };
};

const renderComponent = (tournament: ITournamentResponse) =>
  render(<TournamentDivisionAssignment tournament={tournament} />);

/** Opens the roster "Inscribir equipos" picker for a division region and returns the dialog. */
const openRosterPicker = async (
  user: ReturnType<typeof userEvent.setup>,
  region: HTMLElement
) => {
  await user.click(
    within(region).getByRole('button', { name: /inscribir equipos/i })
  );
  return screen.findByRole('dialog');
};

/** Opens the "Agregar equipos" sub-group picker for a stage region and returns the dialog. */
const openStagePicker = async (
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
      groupStagesByDivision: { 'div-a': [buildGroupStage('gs-a', 'div-a')] },
      enrolled: [buildTeam('river-id', 'River')],
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    expect(
      await screen.findByText(/inscribiendo equipos en cada división como borrador/i)
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

describe('TournamentDivisionAssignment — playoffs-only division bug fix (D8)', () => {
  it('renders an enrol widget for a division with NO group stage, instead of nothing', async () => {
    const zoneA = buildDivision('div-a', 'Zona A');
    const { tournament } = setup({
      divisions: [zoneA],
      // No entry in groupStagesByDivision or eliminationStagesByDivision:
      // a genuinely groupless, bracket-less division (e.g. right after
      // enabling it via the wizard with hasGroupStage unchecked).
      enrolled: [buildTeam('river-id', 'River')],
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    // The bug: the old dead fallback rendered NOTHING assignable here. Now an
    // enrol widget is always present.
    expect(
      within(region).getByRole('button', { name: /inscribir equipos/i })
    ).toBeInTheDocument();
    expect(
      within(region).getByText(/todavía no tiene una llave de playoffs configurada/i)
    ).toBeInTheDocument();
  });

  it('shows a "Sortear llave" trigger for a playoffs-only division once it has a bracket stage', async () => {
    const zoneA = buildDivision('div-a', 'Zona A');
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const finalStage = buildEliminationStage('final-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      eliminationStagesByDivision: { 'div-a': [finalStage] },
      rosterByDivision: { 'div-a': [river, boca] },
      enrolled: [river, boca],
      completability: { canStart: false, issues: [] },
    });

    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    expect(
      await within(region).findByRole('button', { name: /sortear llave/i })
    ).toBeEnabled();
  });
});

describe('TournamentDivisionAssignment — roster enrollment (HU-107/108)', () => {
  it('enrolling a team calls the roster endpoint, not a stage assignment', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');

    const { tournament } = setup({
      divisions: [zoneA],
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    const dialog = await openRosterPicker(user, region);

    await user.click(within(dialog).getByText('River'));
    await user.click(within(dialog).getByRole('button', { name: /agregar \(1\)/i }));

    await waitFor(() => expect(enrollTeams).toHaveBeenCalledTimes(1));
    expect(enrollTeams).toHaveBeenCalledWith(zoneA.id, [river.id]);
    expect(assignTeamsToStage).not.toHaveBeenCalled();
  });

  it('unenrolling an UNPLACED team removes it immediately, no confirmation dialog', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(
      within(region).getByRole('button', { name: /quitar river de la división/i })
    );

    await waitFor(() => expect(unenrollTeams).toHaveBeenCalledTimes(1));
    expect(unenrollTeams).toHaveBeenCalledWith(zoneA.id, [river.id]);
    expect(mockedSwalFire).not.toHaveBeenCalled();
  });

  it('unenrolling a PLACED team shows a cascade-confirmation dialog before removing it', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [river] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(
      within(region).getByRole('button', { name: /quitar river de la división/i })
    );

    await waitFor(() => expect(mockedSwalFire).toHaveBeenCalledTimes(1));
    expect(mockedSwalFire.mock.calls[0][0]).toMatchObject({
      title: 'Quitar equipo de la división',
    });
    await waitFor(() => expect(unenrollTeams).toHaveBeenCalledWith(zoneA.id, [river.id]));
  });

  it('does not unenroll when the cascade confirmation is dismissed', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    mockedSwalFire.mockResolvedValueOnce({
      isConfirmed: false,
      isDenied: false,
      isDismissed: true,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [river] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(
      within(region).getByRole('button', { name: /quitar river de la división/i })
    );

    await waitFor(() => expect(mockedSwalFire).toHaveBeenCalledTimes(1));
    expect(unenrollTeams).not.toHaveBeenCalled();
  });
});

describe('TournamentDivisionAssignment — sub-group placement (HU-121/122)', () => {
  it('the sub-group picker eligible pool is the roster minus already-placed, not enrolled-tournament-teams-minus-other-zones', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      // Boca is tournament-enrolled but NOT on Zona A's roster — must not be
      // offered here, even though the old "enrolled tournament teams" pool
      // would have included it.
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [] },
      enrolled: [river, boca],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    const dialog = await openStagePicker(user, region);

    expect(within(dialog).getByText('River')).toBeInTheDocument();
    expect(within(dialog).queryByText('Boca')).not.toBeInTheDocument();
  });

  it('assigns a roster team to its sub-group stage', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    const dialog = await openStagePicker(user, region);

    await user.click(within(dialog).getByText('River'));
    await user.click(within(dialog).getByRole('button', { name: /agregar \(1\)/i }));

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(groupA.id, [river.id]);
  });

  it('removes (unassigns) a team from its sub-group without touching the roster', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [river] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(within(region).getByRole('button', { name: /^quitar river$/i }));

    await waitFor(() => expect(unassignTeamsFromStage).toHaveBeenCalledTimes(1));
    expect(unassignTeamsFromStage).toHaveBeenCalledWith(groupA.id, [river.id]);
    expect(unenrollTeams).not.toHaveBeenCalled();

    // The roster panel still lists River — unassigning from a sub-group is
    // NOT the same as unenrolling from the division.
    expect(
      within(region).getByRole('button', { name: /quitar river de la división/i })
    ).toBeInTheDocument();
  });

  it('manually moves a team from one sub-group to another via the reassign action', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a', 'Grupo A', 1);
    const groupB = buildGroupStage('gs-b', 'div-a', 'Grupo B', 2);

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA, groupB] },
      assignedByStage: { 'gs-a': [river], 'gs-b': [] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const grupoARegion = await screen.findByRole('region', { name: /^Grupo A$/i });
    await user.click(
      within(grupoARegion).getByRole('button', {
        name: /mover river a otro sub-grupo/i,
      })
    );

    const menu = await screen.findByRole('menu');
    await user.click(within(menu).getByRole('menuitem', { name: /grupo b/i }));

    await waitFor(() => expect(reassignTeamToSubGroup).toHaveBeenCalledTimes(1));
    expect(reassignTeamToSubGroup).toHaveBeenCalledWith(
      zoneA.id,
      river.id,
      groupA.id,
      groupB.id
    );
  });

  it('"Auto-repartir" calls autoDistribute and refetches the division', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(within(region).getByRole('button', { name: /auto-repartir/i }));

    await waitFor(() => expect(autoDistribute).toHaveBeenCalledTimes(1));
    expect(autoDistribute).toHaveBeenCalledWith(zoneA.id);
    // Refetches: getRoster/getStagesByFilters run again for this division.
    await waitFor(() => expect(getRoster).toHaveBeenCalledTimes(2));
  });
});

describe('TournamentDivisionAssignment — HU-123 edit sub-group count', () => {
  it('confirming the rebuild dialog calls rebuildSubGroups with the new count and refetches', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');
    const groupA = buildGroupStage('gs-a', 'div-a');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: { 'div-a': [river] },
      groupStagesByDivision: { 'div-a': [groupA] },
      assignedByStage: { 'gs-a': [river] },
      enrolled: [river],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const region = await screen.findByRole('region', { name: /Zona A/i });
    await user.click(
      within(region).getByRole('button', { name: /editar cantidad de sub-grupos/i })
    );

    const dialog = await screen.findByRole('dialog');
    const input = within(dialog).getByLabelText(/cantidad de sub-grupos/i);
    await user.clear(input);
    await user.type(input, '3');
    await user.click(within(dialog).getByRole('button', { name: /confirmar/i }));

    await waitFor(() => expect(rebuildSubGroups).toHaveBeenCalledTimes(1));
    expect(rebuildSubGroups).toHaveBeenCalledWith(zoneA.id, 3);
    await waitFor(() => expect(getRoster).toHaveBeenCalledTimes(2));
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
      rosterByDivision: { 'div-c': [river, boca] },
      groupStagesByDivision: { 'div-c': [grupo1, grupo2] },
      assignedByStage: { 'gs-c1': [], 'gs-c2': [] },
      enrolled: [river, boca],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const grupo2Region = await screen.findByRole('region', { name: /^Grupo 2$/i });
    const dialog = await openStagePicker(user, grupo2Region);
    await user.click(within(dialog).getByText('River'));
    await user.click(within(dialog).getByRole('button', { name: /agregar \(1\)/i }));

    await waitFor(() => expect(assignTeamsToStage).toHaveBeenCalledTimes(1));
    expect(assignTeamsToStage).toHaveBeenCalledWith(grupo2.id, [river.id]);
  });

  it('excludes a team already placed in Grupo 1 from Grupo 2\'s eligible pool', async () => {
    const river = buildTeam('river-id', 'River');
    const boca = buildTeam('boca-id', 'Boca');
    const crossCup = buildDivision('div-c', 'Copa Club12', true);
    const grupo1 = buildGroupStage('gs-c1', 'div-c', 'Grupo 1', 1);
    const grupo2 = buildGroupStage('gs-c2', 'div-c', 'Grupo 2', 2);

    const { tournament } = setup({
      divisions: [crossCup],
      rosterByDivision: { 'div-c': [river, boca] },
      groupStagesByDivision: { 'div-c': [grupo1, grupo2] },
      assignedByStage: { 'gs-c1': [river], 'gs-c2': [] },
      enrolled: [river, boca],
      completability: { canStart: false, issues: [] },
    });

    const user = userEvent.setup();
    renderComponent(tournament);

    const grupo2Region = await screen.findByRole('region', { name: /^Grupo 2$/i });
    const dialog = await openStagePicker(user, grupo2Region);

    expect(within(dialog).queryByText('River')).not.toBeInTheDocument();
    expect(within(dialog).getByText('Boca')).toBeInTheDocument();
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

  it('a team not enrolled in ANY division roster counts as unassigned and blocks start', async () => {
    const river = buildTeam('river-id', 'River');
    const zoneA = buildDivision('div-a', 'Zona A');

    const { tournament } = setup({
      divisions: [zoneA],
      rosterByDivision: {},
      enrolled: [river],
      completability: { canStart: true, issues: [] },
    });

    renderComponent(tournament);

    expect(
      await screen.findByText(/hay 1 equipo\(s\) sin ninguna división asignada/i)
    ).toBeInTheDocument();
    const startButton = screen.getByRole('button', { name: /iniciar torneo/i });
    expect(startButton).toBeDisabled();
  });
});
