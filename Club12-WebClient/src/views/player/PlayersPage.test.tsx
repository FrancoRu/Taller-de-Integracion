import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import PlayersPage from '@/views/player/PlayersPage';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import type { IPlayerContextProps, IPlayerResponse } from '@/modules/player/type/player.d';
import type { GUID } from '@/modules/core/types/types';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';

vi.mock('@/modules/player/hook/player.hook');
vi.mock('@/modules/team/hook/team.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  confirmDelete: vi.fn().mockResolvedValue(false),
  notifyError: vi.fn(),
  notifySuccess: vi.fn(),
  notifyWarning: vi.fn(),
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual =
    await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockedUsePlayer = vi.mocked(usePlayer);
const mockedUseTeam = vi.mocked(useTeam);

const PLAYER: IPlayerResponse = {
  id: '11111111-1111-1111-1111-111111111111' as GUID,
  slug: 'lopez-carlos',
  fullName: 'LÓPEZ Carlos',
  firstName: 'Carlos',
  secondName: '',
  lastName: 'López',
  documentNumber: '30000001',
  birthDate: new Date('2000-01-01'),
  phoneNumber: '3510000000',
  socialSecurity: 'OSDE',
  teamId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID,
  isFederated: false,
  club: '',
  category: '',
};

let getPlayersByFilter: Mock<IPlayerContextProps['getPlayersByFilter']>;

const setupHooks = (players: IPlayerResponse[] = [PLAYER]) => {
  getPlayersByFilter = vi.fn<IPlayerContextProps['getPlayersByFilter']>();
  getPlayersByFilter.mockResolvedValue({
    items: players,
    page: 1,
    pageSize: TABLE_ROWS_PER_PAGE,
    totalCount: players.length,
  });

  mockedUsePlayer.mockReturnValue({
    player: null,
    players,
    addPlayer: vi.fn(),
    getPlayerById: vi.fn(),
    getPlayersByFilter,
    putPlayerById: vi.fn(),
    deletePlayerById: vi.fn().mockResolvedValue({ success: true }),
    registerPlayerToTeam: vi.fn(),
  } as unknown as IPlayerContextProps);

  mockedUseTeam.mockReturnValue({
    teams: [],
    getTeamsByFiltered: vi.fn().mockResolvedValue(undefined),
  } as unknown as ReturnType<typeof useTeam>);
};

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

/**
 * MUI DataGrid virtualizes on measured container size; jsdom reports zero
 * dimensions, which hides the actions column. Stub non-zero dimensions.
 */
const stubLayoutDimensions = () => {
  Object.defineProperties(window.HTMLElement.prototype, {
    offsetWidth: { configurable: true, get: () => 1000 },
    offsetHeight: { configurable: true, get: () => 1000 },
    clientWidth: { configurable: true, get: () => 1000 },
    clientHeight: { configurable: true, get: () => 1000 },
  });
  window.HTMLElement.prototype.getBoundingClientRect = () =>
    ({
      width: 1000,
      height: 1000,
      top: 0,
      left: 0,
      right: 1000,
      bottom: 1000,
      x: 0,
      y: 0,
      toJSON() {},
    }) as DOMRect;
};

const renderPlayersPage = () =>
  render(
    <MemoryRouter>
      <PlayersPage />
    </MemoryRouter>
  );

beforeEach(() => {
  setupHooks();
  if (!window.ResizeObserver) {
    window.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;
  }
  stubLayoutDimensions();
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('PlayersPage — "Ver" action', () => {
  it('navigates to /panel/jugadores/{slug} using the row slug, not its id', async () => {
    renderPlayersPage();

    const viewIcon = await screen.findByTestId('VisibilityIcon');
    fireEvent.click(viewIcon.closest('button') as HTMLButtonElement);

    await waitFor(() =>
      expect(mockNavigate).toHaveBeenCalledWith('/panel/jugadores/lopez-carlos')
    );
    expect(mockNavigate).not.toHaveBeenCalledWith(
      `/panel/jugadores/${PLAYER.id}`
    );
  });
});

describe('PlayersPage — "Equipo" column', () => {
  it('shows it in the global list, where a row could belong to any team', async () => {
    renderPlayersPage();

    expect(await screen.findByRole('columnheader', { name: 'Equipo' })).toBeInTheDocument();
  });

  it('omits it inside a team roster, where every row is already the same team', async () => {
    render(
      <MemoryRouter>
        <PlayersPage teamId={PLAYER.teamId} />
      </MemoryRouter>
    );

    await screen.findByText(PLAYER.firstName);
    expect(screen.queryByRole('columnheader', { name: 'Equipo' })).not.toBeInTheDocument();
  });
});

describe('PlayersPage — resilient rendering', () => {
  it('renders a row missing documentNumber as "—" instead of crashing', async () => {
    setupHooks([{ ...PLAYER, documentNumber: undefined as unknown as string }]);
    renderPlayersPage();

    expect(await screen.findByText('Carlos')).toBeInTheDocument();
    expect(screen.getByText('López')).toBeInTheDocument();
    expect(screen.getAllByText('—').length).toBeGreaterThan(0);
  });
});

describe('PlayersPage — list actions', () => {
  it('does not offer an Editar row action — editing lives inside the player detail page', async () => {
    renderPlayersPage();

    await screen.findByTestId('VisibilityIcon');
    expect(screen.queryByTestId('EditIcon')).not.toBeInTheDocument();
  });
});

describe('PlayersPage — Dorsal action (roster context)', () => {
  const TOURNAMENT_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb' as GUID;

  it('is absent outside a team/tournament roster context', async () => {
    renderPlayersPage();

    await screen.findByTestId('VisibilityIcon');
    expect(screen.queryByTestId('NumbersIcon')).not.toBeInTheDocument();
  });

  it('saves the dorsal via registerPlayerToTeam when team+tournament are in scope', async () => {
    const registerPlayerToTeam = vi
      .fn()
      .mockResolvedValue({ success: true, data: {} });
    mockedUsePlayer.mockReturnValue({
      player: null,
      players: [PLAYER],
      addPlayer: vi.fn(),
      getPlayerById: vi.fn(),
      getPlayersByFilter,
      putPlayerById: vi.fn(),
      deletePlayerById: vi.fn().mockResolvedValue({ success: true }),
      registerPlayerToTeam,
    } as unknown as IPlayerContextProps);

    render(
      <MemoryRouter>
        <PlayersPage teamId={PLAYER.teamId} tournamentId={TOURNAMENT_ID} />
      </MemoryRouter>
    );

    const dorsalIcon = await screen.findByTestId('NumbersIcon');
    fireEvent.click(dorsalIcon.closest('button') as HTMLButtonElement);

    const dorsalInput = await screen.findByRole('spinbutton', {
      name: 'Dorsal',
    });
    fireEvent.change(dorsalInput, { target: { value: '7' } });

    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() =>
      expect(registerPlayerToTeam).toHaveBeenCalledWith(
        PLAYER.id,
        expect.objectContaining({
          teamId: PLAYER.teamId,
          tournamentId: TOURNAMENT_ID,
          jerseyNumber: 7,
        })
      )
    );
  });
});

describe('PlayersPage — "Nuevo Jugador" adds an inline editable row (tabla editable)', () => {
  it('adds a draft row with Guardar/Descartar actions, and Descartar removes it', async () => {
    renderPlayersPage();

    fireEvent.click(screen.getByRole('button', { name: 'Nuevo Jugador' }));

    await screen.findByTestId('CheckIcon');
    const discardButton = screen
      .getByTestId('CloseIcon')
      .closest('button') as HTMLButtonElement;

    fireEvent.click(discardButton);

    await waitFor(() =>
      expect(screen.queryByTestId('CheckIcon')).not.toBeInTheDocument()
    );
  });

  it('saves a filled-in draft row via addPlayer instead of opening a popup form', async () => {
    const addPlayer = vi.fn().mockResolvedValue({ ...PLAYER, id: 'new-id' as GUID });
    const getPlayersByFilterEmpty = vi.fn().mockResolvedValue({
      items: [],
      page: 1,
      pageSize: TABLE_ROWS_PER_PAGE,
      totalCount: 0,
    });
    mockedUsePlayer.mockReturnValue({
      player: null,
      players: [],
      addPlayer,
      getPlayerById: vi.fn(),
      getPlayersByFilter: getPlayersByFilterEmpty,
      putPlayerById: vi.fn(),
      deletePlayerById: vi.fn().mockResolvedValue({ success: true }),
      registerPlayerToTeam: vi.fn(),
    } as unknown as IPlayerContextProps);

    // Rendered with a fixed teamId (a team's roster context), so the "Equipo"
    // column isn't shown at all and the draft row's team is implied —
    // matching how this component is actually used from a team's roster tab.
    const { container } = render(
      <MemoryRouter>
        <PlayersPage teamId={PLAYER.teamId} />
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Nuevo Jugador' }));
    await screen.findByTestId('CheckIcon');

    // `apiRef.setEditCellValue` commits asynchronously, so each field is
    // awaited before moving to the next one — otherwise a later change can
    // race ahead of an earlier one still being processed.
    const setCellValue = async (field: string, value: string) => {
      const input = container.querySelector(
        `[data-field="${field}"] input`
      ) as HTMLInputElement;
      fireEvent.change(input, { target: { value } });
      await waitFor(() => expect(input).toHaveValue(value));
    };

    await setCellValue('firstName', 'Nueva');
    await setCellValue('lastName', 'Persona');
    await setCellValue('documentNumber', '30111222');
    await setCellValue('birthDate', '2000-05-05');
    await setCellValue('phoneNumber', '3511234567');
    await setCellValue('socialSecurity', 'OSDE');

    fireEvent.click(
      screen.getByTestId('CheckIcon').closest('button') as HTMLButtonElement
    );

    await waitFor(() =>
      expect(addPlayer).toHaveBeenCalledWith(
        expect.objectContaining({
          firstName: 'Nueva',
          lastName: 'Persona',
          documentNumber: '30111222',
          phoneNumber: '3511234567',
          socialSecurity: 'OSDE',
          teamId: PLAYER.teamId,
        })
      )
    );
  });
});
