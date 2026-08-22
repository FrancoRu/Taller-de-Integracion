import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import DataAdministrationPage from '@/views/panel/DataAdministrationPage';
import PrivateRoute from '@/views/core/privateRoute';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { useBackups } from '@/modules/backup/hook/backup.hook';
import type { UseBackupsResult } from '@/modules/backup/hook/backup.hook';
import { dataMaintenanceService } from '@/modules/dataMaintenance/service/dataMaintenance.service';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

vi.mock('@/modules/auth/hook/auth.hook');
vi.mock('@/modules/backup/hook/backup.hook');
vi.mock('@/modules/dataMaintenance/service/dataMaintenance.service');
vi.mock('@/views/panel/components/BackupsTable', () => ({
  default: () => <div data-testid="backups-table-stub" />,
}));
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
  },
}));

import Swal from 'sweetalert2';

const mockedUseAuth = vi.mocked(useAuth);
const mockedUseBackups = vi.mocked(useBackups);
const mockedDataMaintenanceService = vi.mocked(dataMaintenanceService, true);
const mockedSwalFire = vi.mocked(Swal.fire);

const buildBackupsHookValue = (
  overrides: Partial<UseBackupsResult> = {}
): UseBackupsResult => ({
  backups: [],
  loading: false,
  busy: false,
  fetchBackups: vi.fn(),
  createBackup: vi.fn().mockResolvedValue(true),
  deleteBackup: vi.fn().mockResolvedValue(true),
  restoreBackup: vi.fn().mockResolvedValue(true),
  ...overrides,
});

beforeEach(() => {
  mockedUseBackups.mockReturnValue(buildBackupsHookValue());
  mockedSwalFire.mockResolvedValue({
    isConfirmed: true,
    isDenied: false,
    isDismissed: false,
  } as Awaited<ReturnType<typeof Swal.fire>>);
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('DataAdministrationPage — layout', () => {
  it('renders the title and two cards, "Base de datos" above "Test"', () => {
    render(
      <MemoryRouter>
        <DataAdministrationPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Administración de datos')).toBeInTheDocument();

    const baseDeDatos = screen.getByText('Base de datos');
    const test = screen.getByText('Test');
    expect(baseDeDatos).toBeInTheDocument();
    expect(test).toBeInTheDocument();
    expect(
      baseDeDatos.compareDocumentPosition(test) &
        Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();

    expect(screen.getByRole('button', { name: 'Borrar los datos' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Generar respaldo' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Cargar Datos de prueba' })).toBeInTheDocument();
    expect(screen.getByTestId('backups-table-stub')).toBeInTheDocument();
  });

  it('fetches backups on mount', () => {
    const fetchBackups = vi.fn();
    mockedUseBackups.mockReturnValue(buildBackupsHookValue({ fetchBackups }));

    render(
      <MemoryRouter>
        <DataAdministrationPage />
      </MemoryRouter>
    );

    expect(fetchBackups).toHaveBeenCalledTimes(1);
  });
});

describe('DataAdministrationPage — Generar respaldo', () => {
  it('calls createBackup when clicked', async () => {
    const createBackup = vi.fn().mockResolvedValue(true);
    mockedUseBackups.mockReturnValue(buildBackupsHookValue({ createBackup }));

    render(
      <MemoryRouter>
        <DataAdministrationPage />
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Generar respaldo' }));

    await waitFor(() => expect(createBackup).toHaveBeenCalledTimes(1));
  });
});

describe('DataAdministrationPage — Test card seed action', () => {
  it('seeds test data exactly as before the panel rename', async () => {
    mockedDataMaintenanceService.seedSampleData.mockResolvedValueOnce({
      data: { tournaments: 2, divisions: 0, teams: 4, players: 20, matches: 0, playerSanctions: 0, blogPosts: 0 },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {},
    } as never);

    render(
      <MemoryRouter>
        <DataAdministrationPage />
      </MemoryRouter>
    );

    fireEvent.click(screen.getByRole('button', { name: 'Cargar Datos de prueba' }));

    await waitFor(() =>
      expect(mockedDataMaintenanceService.seedSampleData).toHaveBeenCalledTimes(1)
    );
  });
});

describe('DataAdministrationPage — Admin-only guard', () => {
  const renderGuarded = () =>
    render(
      <MemoryRouter initialEntries={[APP_ROUTES.panelDataAdministration]}>
        <Routes>
          <Route path={APP_ROUTES.forbidden} element={<div>Acceso denegado</div>} />
          <Route
            path={APP_ROUTES.panelDataAdministration}
            element={
              <PrivateRoute allowedRoles={[UserRolesType.Admin]}>
                <DataAdministrationPage />
              </PrivateRoute>
            }
          />
        </Routes>
      </MemoryRouter>
    );

  it('renders the panel for an Admin', () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      role: UserRolesType.Admin,
    } as ReturnType<typeof useAuth>);

    renderGuarded();

    expect(screen.getByText('Administración de datos')).toBeInTheDocument();
  });

  it('denies access to a non-Admin', () => {
    mockedUseAuth.mockReturnValue({
      isAuthenticated: true,
      role: UserRolesType.TeamManager,
    } as ReturnType<typeof useAuth>);

    renderGuarded();

    expect(screen.getByText('Acceso denegado')).toBeInTheDocument();
    expect(
      screen.queryByText('Administración de datos')
    ).not.toBeInTheDocument();
  });
});
