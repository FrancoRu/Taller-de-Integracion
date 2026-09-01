import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import TeamStaffManager from '@/views/team/TeamStaffManager';
import { useTeamStaff } from '@/modules/teamStaff/hook/teamStaff.hook';
import type { UseTeamStaff } from '@/modules/teamStaff/hook/teamStaff.hook';
import { ITeamStaffResponse } from '@/modules/teamStaff/type/teamStaff';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/teamStaff/hook/teamStaff.hook');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  confirmDelete: vi.fn().mockResolvedValue(true),
  notifySuccess: vi.fn().mockResolvedValue(undefined),
  notifyError: vi.fn().mockResolvedValue(undefined),
  notifyWarning: vi.fn().mockResolvedValue(undefined),
}));

const mockedUseTeamStaff = vi.mocked(useTeamStaff);
const mockedConfirmDelete = vi.mocked(confirmDelete);
const mockedNotifySuccess = vi.mocked(notifySuccess);
const mockedNotifyError = vi.mocked(notifyError);
const mockedNotifyWarning = vi.mocked(notifyWarning);

const TEAM_ID = 'team-1' as unknown as GUID;
const TOURNAMENT_ID = 'tournament-1' as unknown as GUID;

const buildStaff = (
  overrides: Partial<ITeamStaffResponse> = {}
): ITeamStaffResponse => ({
  id: 'staff-1' as unknown as GUID,
  teamId: TEAM_ID,
  tournamentId: TOURNAMENT_ID,
  fullName: 'Juan Pérez',
  role: 'Coach',
  dateCreated: '2026-01-01T00:00:00Z',
  ...overrides,
});

let create: Mock<UseTeamStaff['create']>;
let remove: Mock<UseTeamStaff['remove']>;

const setupHook = (staff: ITeamStaffResponse[]) => {
  create = vi.fn<UseTeamStaff['create']>();
  create.mockResolvedValue(buildStaff());
  remove = vi.fn<UseTeamStaff['remove']>();
  remove.mockResolvedValue(undefined);

  mockedUseTeamStaff.mockReturnValue({
    staff,
    loading: false,
    refresh: vi.fn(),
    create,
    remove,
  });
};

const renderManager = () =>
  render(<TeamStaffManager teamId={TEAM_ID} tournamentId={TOURNAMENT_ID} />);

afterEach(() => {
  vi.clearAllMocks();
});

describe('TeamStaffManager', () => {
  it('renders the staff list with name and role label', () => {
    setupHook([buildStaff({ fullName: 'Juan Pérez', role: 'Coach' })]);

    renderManager();

    expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
    expect(screen.getByText('DT')).toBeInTheDocument();
  });

  it('shows a quiet empty state when there is no staff', () => {
    setupHook([]);

    renderManager();

    expect(
      screen.getByText(/no tiene cuerpo técnico/i)
    ).toBeInTheDocument();
  });

  it('opens the add dialog, submits it, and creates the staff member', async () => {
    const user = userEvent.setup();
    setupHook([]);

    renderManager();

    await user.click(screen.getByRole('button', { name: /agregar/i }));

    const dialog = screen.getByRole('dialog');
    await user.type(
      within(dialog).getByLabelText(/nombre completo/i),
      'María López'
    );

    await user.click(within(dialog).getByRole('combobox', { name: /rol/i }));
    await user.click(screen.getByRole('option', { name: 'Asistente' }));

    await user.click(within(dialog).getByRole('button', { name: /agregar/i }));

    await waitFor(() =>
      expect(create).toHaveBeenCalledWith({
        fullName: 'María López',
        role: 'AssistantCoach',
        tournamentId: TOURNAMENT_ID,
      })
    );
    await waitFor(() =>
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    );
    expect(mockedNotifySuccess).toHaveBeenCalled();
  });

  it('warns and does not submit when the name is empty', async () => {
    const user = userEvent.setup();
    setupHook([]);

    renderManager();

    await user.click(screen.getByRole('button', { name: /agregar/i }));
    const dialog = screen.getByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: /agregar/i }));

    expect(mockedNotifyWarning).toHaveBeenCalled();
    expect(create).not.toHaveBeenCalled();
  });

  it('removes a staff member after confirming', async () => {
    const user = userEvent.setup();
    setupHook([buildStaff({ fullName: 'Juan Pérez' })]);

    renderManager();

    await user.click(
      screen.getByRole('button', { name: /quitar.*juan pérez/i })
    );

    await waitFor(() => expect(mockedConfirmDelete).toHaveBeenCalled());
    await waitFor(() => expect(remove).toHaveBeenCalledWith('staff-1'));
    expect(mockedNotifySuccess).toHaveBeenCalled();
  });

  it('does not remove a staff member when confirmation is cancelled', async () => {
    const user = userEvent.setup();
    mockedConfirmDelete.mockResolvedValueOnce(false);
    setupHook([buildStaff({ fullName: 'Juan Pérez' })]);

    renderManager();

    await user.click(
      screen.getByRole('button', { name: /quitar.*juan pérez/i })
    );

    await waitFor(() => expect(mockedConfirmDelete).toHaveBeenCalled());
    expect(remove).not.toHaveBeenCalled();
  });

  it('shows an error notification when creation fails', async () => {
    const user = userEvent.setup();
    setupHook([]);
    create.mockRejectedValueOnce(new Error('fail'));

    renderManager();

    await user.click(screen.getByRole('button', { name: /agregar/i }));
    const dialog = screen.getByRole('dialog');
    await user.type(
      within(dialog).getByLabelText(/nombre completo/i),
      'María López'
    );
    await user.click(within(dialog).getByRole('button', { name: /agregar/i }));

    await waitFor(() => expect(mockedNotifyError).toHaveBeenCalled());
  });
});
