import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, afterEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import TournamentEnrolledTeams from '@/views/tournament/TournamentEnrolledTeams';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import type { ITeamContextProps, ITeamResponse } from '@/modules/team/type/team.d';
import type { ITournamentContextProps } from '@/modules/tournament/type/tournament.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/tournament/hook/tournament.hook');
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn() },
}));

import Swal from 'sweetalert2';

const mockedUseTeam = vi.mocked(useTeam);
const mockedUseTournament = vi.mocked(useTournament);
const mockedSwalFire = vi.mocked(Swal.fire);

const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

let guidCounter = 0;
const nextGuid = (): GUID =>
  `guid-${guidCounter++}-aaaa-bbbb-cccc` as unknown as GUID;

const buildTeam = (overrides: Partial<ITeamResponse> = {}): ITeamResponse => ({
  id: nextGuid(),
  name: 'River',
  slug: 'river',
  threeLetterCode: 'RIV',
  shirtColor: 'Rojo',
  logoUrl: '',
  players: [],
  tournamentId: null,
  ...overrides,
});

let getTeamsByFiltered: Mock<ITeamContextProps['getTeamsByFiltered']>;
let addTeam: Mock<ITeamContextProps['addTeam']>;
let enrollTeam: Mock<ITournamentContextProps['enrollTeam']>;
let unenrollTeam: Mock<ITournamentContextProps['unenrollTeam']>;

const setup = (options: {
  enrolled?: ITeamResponse[];
  all?: ITeamResponse[];
}) => {
  const enrolled = options.enrolled ?? [];
  const all = options.all ?? [];

  getTeamsByFiltered = vi.fn<ITeamContextProps['getTeamsByFiltered']>();
  getTeamsByFiltered.mockImplementation(async filter =>
    filter?.tournamentId
      ? { items: enrolled, page: 1, pageSize: 300, totalCount: enrolled.length }
      : { items: all, page: 1, pageSize: 300, totalCount: all.length }
  );

  addTeam = vi.fn<ITeamContextProps['addTeam']>();

  enrollTeam = vi.fn<ITournamentContextProps['enrollTeam']>();
  enrollTeam.mockResolvedValue(true);

  unenrollTeam = vi.fn<ITournamentContextProps['unenrollTeam']>();
  unenrollTeam.mockResolvedValue(true);

  mockedUseTeam.mockReturnValue({
    team: null,
    teams: null,
    addTeam,
    putTeamById: vi.fn(),
    putTeamLogoById: vi.fn(),
    getTeamsByFiltered,
    getTeamById: vi.fn(),
    deleteTeamById: vi.fn(),
  } as ITeamContextProps);

  mockedUseTournament.mockReturnValue({
    tournament: null,
    tournaments: null,
    addTournament: vi.fn(),
    createFullTournament: vi.fn(),
    addFullDivision: vi.fn(),
    getAllTournamentsByFilter: vi.fn(),
    getTournamentById: vi.fn(),
    putTournamentById: vi.fn(),
    deleteTournamentById: vi.fn(),
    registerTeamsByTournamentId: vi.fn(),
    enrollTeam,
    unenrollTeam,
    getCompletability: vi.fn(),
  } as ITournamentContextProps);
};

const renderComponent = () =>
  render(
    <MemoryRouter>
      <TournamentEnrolledTeams tournamentId={TOURNAMENT_ID} />
    </MemoryRouter>
  );

const openDialog = async (user: ReturnType<typeof userEvent.setup>) => {
  await user.click(screen.getByRole('button', { name: /inscribir equipo/i }));
  return screen.getByRole('dialog');
};

beforeEach(() => {
  guidCounter = 0;
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('TournamentEnrolledTeams — enrolled list', () => {
  it('fetches enrolled teams scoped to the tournament and links each to its roster', async () => {
    const river = buildTeam({ name: 'River', slug: 'river' });
    setup({ enrolled: [river], all: [river] });

    renderComponent();

    await waitFor(() =>
      expect(getTeamsByFiltered).toHaveBeenCalledWith(
        expect.objectContaining({ tournamentId: TOURNAMENT_ID })
      )
    );

    const rosterLink = await screen.findByRole('link', { name: 'River' });
    expect(rosterLink).toHaveAttribute('href', '/panel/equipos/river');
  });

  it('shows an empty message when no team is enrolled yet', async () => {
    setup({ enrolled: [], all: [] });

    renderComponent();

    expect(
      await screen.findByText(/todavía no hay equipos inscriptos/i)
    ).toBeInTheDocument();
  });
});

describe('TournamentEnrolledTeams — enroll new team', () => {
  it('creates a new team with its identity fields, then enrolls it', async () => {
    setup({ enrolled: [], all: [] });
    const created = buildTeam({ name: 'Racing', threeLetterCode: 'RAC' });
    addTeam.mockResolvedValue(created);
    const user = userEvent.setup();

    renderComponent();
    await screen.findByText(/todavía no hay equipos inscriptos/i);

    const dialog = await openDialog(user);
    await user.type(
      within(dialog).getByRole('textbox', { name: /^nombre$/i }),
      'Racing'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /código/i }),
      'rac'
    );
    const logoFile = new File(['logo'], 'racing.png', { type: 'image/png' });
    const fileInput = dialog.querySelector('input[type="file"]') as HTMLInputElement;
    await user.upload(fileInput, logoFile);

    await user.click(within(dialog).getByRole('button', { name: /inscribir/i }));

    await waitFor(() => expect(addTeam).toHaveBeenCalledTimes(1));
    expect(addTeam).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Racing',
        threeLetterCode: 'RAC',
        logo: logoFile,
      })
    );
    await waitFor(() => expect(enrollTeam).toHaveBeenCalledTimes(1));
    expect(enrollTeam).toHaveBeenCalledWith(TOURNAMENT_ID, {
      existingTeamId: created.id,
    });
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Equipo inscripto' })
    );
  });

  it('does not create a team when name/code/logo are missing', async () => {
    setup({ enrolled: [], all: [] });
    const user = userEvent.setup();

    renderComponent();
    const dialog = await openDialog(user);

    await user.click(within(dialog).getByRole('button', { name: /inscribir/i }));

    await waitFor(() =>
      expect(mockedSwalFire).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Campos incompletos' })
      )
    );
    expect(addTeam).not.toHaveBeenCalled();
    expect(enrollTeam).not.toHaveBeenCalled();
  });
});

describe('TournamentEnrolledTeams — enroll existing team', () => {
  it('excludes already-enrolled teams from the existing-team picker', async () => {
    const river = buildTeam({ name: 'River', slug: 'river' });
    const boca = buildTeam({ name: 'Boca', slug: 'boca' });
    setup({ enrolled: [river], all: [river, boca] });
    const user = userEvent.setup();

    renderComponent();
    await screen.findByRole('link', { name: 'River' });

    const dialog = await openDialog(user);
    await user.click(within(dialog).getByRole('radio', { name: /equipo existente/i }));
    await user.click(within(dialog).getByRole('combobox'));

    expect(screen.getByRole('option', { name: 'Boca' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'River' })).not.toBeInTheDocument();
  });

  it('enrolls an existing team copying the roster from its previous season', async () => {
    const previousSeason = 'previous-season' as unknown as GUID;
    const boca = buildTeam({
      name: 'Boca',
      slug: 'boca',
      tournamentId: previousSeason,
    });
    setup({ enrolled: [], all: [boca] });
    const user = userEvent.setup();

    renderComponent();
    const dialog = await openDialog(user);

    await user.click(within(dialog).getByRole('radio', { name: /equipo existente/i }));
    await user.click(within(dialog).getByRole('combobox'));
    await user.click(screen.getByRole('option', { name: 'Boca' }));

    await user.click(
      within(dialog).getByRole('checkbox', {
        name: /copiar plantel de su temporada anterior/i,
      })
    );

    await user.click(within(dialog).getByRole('button', { name: /inscribir/i }));

    await waitFor(() => expect(enrollTeam).toHaveBeenCalledTimes(1));
    expect(enrollTeam).toHaveBeenCalledWith(TOURNAMENT_ID, {
      existingTeamId: boca.id,
      copyRosterFromTournamentId: previousSeason,
    });
  });

  it('enrolls an existing team without copying when the checkbox is unchecked', async () => {
    const boca = buildTeam({
      name: 'Boca',
      slug: 'boca',
      tournamentId: 'previous-season' as unknown as GUID,
    });
    setup({ enrolled: [], all: [boca] });
    const user = userEvent.setup();

    renderComponent();
    const dialog = await openDialog(user);

    await user.click(within(dialog).getByRole('radio', { name: /equipo existente/i }));
    await user.click(within(dialog).getByRole('combobox'));
    await user.click(screen.getByRole('option', { name: 'Boca' }));

    await user.click(within(dialog).getByRole('button', { name: /inscribir/i }));

    await waitFor(() => expect(enrollTeam).toHaveBeenCalledTimes(1));
    expect(enrollTeam).toHaveBeenCalledWith(TOURNAMENT_ID, {
      existingTeamId: boca.id,
    });
  });

  it('does not enroll in existing mode when no team is selected', async () => {
    const boca = buildTeam({ name: 'Boca', slug: 'boca' });
    setup({ enrolled: [], all: [boca] });
    const user = userEvent.setup();

    renderComponent();
    const dialog = await openDialog(user);

    await user.click(within(dialog).getByRole('radio', { name: /equipo existente/i }));
    await user.click(within(dialog).getByRole('button', { name: /inscribir/i }));

    await waitFor(() =>
      expect(mockedSwalFire).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Equipo requerido' })
      )
    );
    expect(enrollTeam).not.toHaveBeenCalled();
  });
});

describe('TournamentEnrolledTeams — unenroll team (HU-108)', () => {
  it('unenrolls a team and refreshes the enrolled list', async () => {
    const river = buildTeam({ name: 'River', slug: 'river' });
    setup({ enrolled: [river], all: [river] });
    const user = userEvent.setup();

    renderComponent();
    await screen.findByRole('link', { name: 'River' });

    getTeamsByFiltered.mockClear();

    await user.click(
      screen.getByRole('button', { name: /dar de baja a River/i })
    );

    await waitFor(() => expect(unenrollTeam).toHaveBeenCalledTimes(1));
    expect(unenrollTeam).toHaveBeenCalledWith(TOURNAMENT_ID, river.id);

    await waitFor(() =>
      expect(getTeamsByFiltered).toHaveBeenCalledWith(
        expect.objectContaining({ tournamentId: TOURNAMENT_ID })
      )
    );
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Equipo dado de baja' })
    );
  });
});
