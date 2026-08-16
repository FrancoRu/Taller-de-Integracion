import { act, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import TeamsPage from '@/views/team/TeamsPage';
import { useTeam } from '@/modules/team/hook/team.hook';
import type { ITeamContextProps } from '@/modules/team/type/team.d';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import type { GUID } from '@/modules/core/types/types';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';

vi.mock('@/modules/team/hook/team.hook');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
  },
}));

import Swal from 'sweetalert2';

const mockedUseTeam = vi.mocked(useTeam);
const mockedSwalFire = vi.mocked(Swal.fire);

let guidCounter = 0;
const nextGuid = (): GUID =>
  `guid-${guidCounter++}-aaaa-bbbb-cccc` as unknown as GUID;

const buildTeam = (overrides: Partial<ITeamResponse> = {}): ITeamResponse => ({
  id: nextGuid(),
  name: 'River',
  threeLetterCode: 'RIV',
  shirtColor: 'Rojo',
  logoUrl: '',
  players: [],
  tournamentId: null,
  ...overrides,
});

let getTeamsByFiltered: Mock<ITeamContextProps['getTeamsByFiltered']>;
let addTeam: Mock<ITeamContextProps['addTeam']>;
let putTeamById: Mock<ITeamContextProps['putTeamById']>;
let deleteTeamById: Mock<ITeamContextProps['deleteTeamById']>;

const setupHook = (teams: ITeamResponse[] = [buildTeam()]) => {
  getTeamsByFiltered = vi.fn<ITeamContextProps['getTeamsByFiltered']>();
  getTeamsByFiltered.mockResolvedValue(undefined);
  addTeam = vi.fn<ITeamContextProps['addTeam']>();
  addTeam.mockResolvedValue(buildTeam());
  putTeamById = vi.fn<ITeamContextProps['putTeamById']>();
  putTeamById.mockResolvedValue(buildTeam());
  deleteTeamById = vi.fn<ITeamContextProps['deleteTeamById']>();
  deleteTeamById.mockResolvedValue(undefined);

  const contextValue: ITeamContextProps = {
    team: null,
    teams,
    addTeam,
    putTeamById,
    putTeamLogoById: vi.fn(),
    getTeamsByFiltered,
    getTeamById: vi.fn(),
    deleteTeamById,
  };

  mockedUseTeam.mockReturnValue(contextValue);
};

const renderTeamsPage = () =>
  render(
    <MemoryRouter>
      <TeamsPage />
    </MemoryRouter>
  );

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

/**
 * MUI DataGrid virtualizes rows/columns based on measured container size.
 * jsdom reports zero layout dimensions, which hides columns like the actions
 * column entirely. Stub non-zero dimensions so all columns render.
 */
const stubLayoutDimensions = () => {
  Object.defineProperties(window.HTMLElement.prototype, {
    offsetWidth: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    offsetHeight: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    clientWidth: {
      configurable: true,
      get() {
        return 1000;
      },
    },
    clientHeight: {
      configurable: true,
      get() {
        return 1000;
      },
    },
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

beforeEach(() => {
  guidCounter = 0;
  setupHook();
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);

  if (!window.ResizeObserver) {
    window.ResizeObserver =
      ResizeObserverStub as unknown as typeof ResizeObserver;
  }

  stubLayoutDimensions();
});

afterEach(() => {
  vi.clearAllMocks();
  vi.useRealTimers();
});

describe('TeamsPage — filter/debounce', () => {
  it('reflects the typed value immediately, debounces the fetch by 1000ms, and resets pagination to page 0', async () => {
    vi.useFakeTimers();
    renderTeamsPage();

    expect(getTeamsByFiltered).toHaveBeenCalledTimes(1);
    getTeamsByFiltered.mockClear();

    const nameInput = screen.getByRole('textbox', { name: 'Nombre' });
    fireEvent.change(nameInput, { target: { value: 'Riv' } });

    expect(nameInput).toHaveValue('Riv');
    expect(getTeamsByFiltered).not.toHaveBeenCalled();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1000);
    });

    expect(getTeamsByFiltered).toHaveBeenCalledTimes(1);
    expect(getTeamsByFiltered).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Riv',
        pageNumber: 1,
        pageSize: TABLE_ROWS_PER_PAGE,
      })
    );
  });
});

describe('TeamsPage — pagination', () => {
  it('fetches the next page with the current page size and shows a loading state mid-fetch', async () => {
    const rows = Array.from({ length: 15 }, (_, index) =>
      buildTeam({
        name: `Team ${index}`,
        threeLetterCode: `T${index}`,
      })
    );
    setupHook(rows);

    let resolveFetch: () => void = () => {};
    getTeamsByFiltered.mockImplementation(
      () =>
        new Promise<void>(resolve => {
          resolveFetch = () => resolve(undefined);
        })
    );

    renderTeamsPage();
    resolveFetch();
    await waitFor(() =>
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    );
    getTeamsByFiltered.mockClear();

    const nextPageButton = screen.getByRole('button', {
      name: /go to next page/i,
    });
    fireEvent.click(nextPageButton);

    expect(screen.getByRole('progressbar')).toBeInTheDocument();

    resolveFetch();
    await waitFor(() =>
      expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
    );

    expect(getTeamsByFiltered).toHaveBeenCalledWith(
      expect.objectContaining({
        pageNumber: 2,
        pageSize: TABLE_ROWS_PER_PAGE,
      })
    );
  });
});

describe('TeamsPage — create dialog', () => {
  it('opens with an empty form', async () => {
    const user = userEvent.setup();
    renderTeamsPage();

    await user.click(
      screen.getByRole('button', { name: /nuevo equipo/i })
    );

    const dialog = screen.getByRole('dialog');
    expect(
      within(dialog).getByRole('textbox', { name: /^Nombre/ })
    ).toHaveValue('');
    expect(
      within(dialog).getByRole('textbox', { name: /^Código/ })
    ).toHaveValue('');
    expect(
      within(dialog).getByRole('textbox', { name: /^Color camiseta/ })
    ).toHaveValue('');
    expect(
      within(dialog).getByRole('button', { name: /seleccionar logo/i })
    ).toBeInTheDocument();
  });

  it('blocks submit without a logo and does not call addTeam', async () => {
    const user = userEvent.setup();
    renderTeamsPage();

    await user.click(
      screen.getByRole('button', { name: /nuevo equipo/i })
    );
    const dialog = screen.getByRole('dialog');

    await user.type(
      within(dialog).getByRole('textbox', { name: /^Nombre/ }),
      'Boca'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Código/ }),
      'boc'
    );

    await user.click(within(dialog).getByRole('button', { name: /crear/i }));

    await waitFor(() =>
      expect(mockedSwalFire).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Logo requerido' })
      )
    );
    expect(addTeam).not.toHaveBeenCalled();
  });

  it('calls addTeam with trimmed values, closes, resets, refetches and shows a success alert', async () => {
    const user = userEvent.setup();
    renderTeamsPage();

    await waitFor(() => expect(getTeamsByFiltered).toHaveBeenCalledTimes(1));
    getTeamsByFiltered.mockClear();

    await user.click(
      screen.getByRole('button', { name: /nuevo equipo/i })
    );
    const dialog = screen.getByRole('dialog');

    await user.type(
      within(dialog).getByRole('textbox', { name: /^Nombre/ }),
      '  Boca  '
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Código/ }),
      'boc'
    );
    await user.type(
      within(dialog).getByRole('textbox', { name: /^Color camiseta/ }),
      ' Azul '
    );

    const logoFile = new File(['logo'], 'logo.png', { type: 'image/png' });
    const fileInput = dialog.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    await user.upload(fileInput, logoFile);

    await user.click(within(dialog).getByRole('button', { name: /crear/i }));

    await waitFor(() => expect(addTeam).toHaveBeenCalledTimes(1));
    expect(addTeam).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Boca',
        threeLetterCode: 'BOC',
        shirtColor: 'Azul',
        logo: logoFile,
      })
    );

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    await waitFor(() => expect(getTeamsByFiltered).toHaveBeenCalledTimes(1));
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Equipo creado' })
    );
  });
});

describe('TeamsPage — edit dialog', () => {
  it('opens prefilled with the selected row values', async () => {
    setupHook([
      buildTeam({ name: 'River', threeLetterCode: 'RIV', shirtColor: 'Rojo' }),
    ]);
    renderTeamsPage();

    const editIcon = await screen.findByTestId('EditIcon');
    fireEvent.click(editIcon.closest('button') as HTMLButtonElement);

    const dialog = screen.getByRole('dialog');
    expect(
      within(dialog).getByRole('textbox', { name: /^Nombre/ })
    ).toHaveValue('River');
    expect(
      within(dialog).getByRole('textbox', { name: /^Código/ })
    ).toHaveValue('RIV');
    expect(
      within(dialog).getByRole('textbox', { name: /^Color camiseta/ })
    ).toHaveValue('Rojo');
  });

  it('calls putTeamById with trimmed values, closes, refetches and shows a success alert', async () => {
    setupHook([
      buildTeam({ name: 'River', threeLetterCode: 'RIV', shirtColor: 'Rojo' }),
    ]);
    const user = userEvent.setup();
    renderTeamsPage();

    await waitFor(() => expect(getTeamsByFiltered).toHaveBeenCalledTimes(1));
    getTeamsByFiltered.mockClear();

    const editIcon = await screen.findByTestId('EditIcon');
    fireEvent.click(editIcon.closest('button') as HTMLButtonElement);

    const dialog = screen.getByRole('dialog');
    const nameInput = within(dialog).getByRole('textbox', {
      name: /^Nombre/,
    });
    await user.clear(nameInput);
    await user.type(nameInput, ' Racing ');

    await user.click(
      within(dialog).getByRole('button', { name: /guardar/i })
    );

    await waitFor(() => expect(putTeamById).toHaveBeenCalledTimes(1));
    const [, payload] = putTeamById.mock.calls[0];
    expect(payload).toEqual(
      expect.objectContaining({ name: 'Racing', threeLetterCode: 'RIV' })
    );

    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    await waitFor(() => expect(getTeamsByFiltered).toHaveBeenCalledTimes(1));
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Equipo actualizado' })
    );
  });
});

describe('TeamsPage — delete confirmation', () => {
  it('does not call deleteTeamById when the confirmation is declined', async () => {
    setupHook([buildTeam()]);
    mockedSwalFire.mockResolvedValueOnce({
      isConfirmed: false,
      isDenied: false,
      isDismissed: true,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    renderTeamsPage();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    fireEvent.click(deleteIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(mockedSwalFire).toHaveBeenCalledTimes(1));
    expect(deleteTeamById).not.toHaveBeenCalled();
  });

  it('calls deleteTeamById with the row id and shows a success alert when confirmed', async () => {
    const team = buildTeam();
    setupHook([team]);

    renderTeamsPage();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    fireEvent.click(deleteIcon.closest('button') as HTMLButtonElement);

    await waitFor(() => expect(deleteTeamById).toHaveBeenCalledWith(team.id));
    expect(mockedSwalFire).toHaveBeenCalledWith(
      expect.objectContaining({ title: '¡Eliminado!' })
    );
  });
});
