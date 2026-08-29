import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import SidebarLayout from '@/views/core/components/SidebarLayout';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

const mockNavigate = vi.fn();

vi.mock('react-router-dom', async (importOriginal) => {
  const actual = await importOriginal<typeof import('react-router-dom')>();
  return { ...actual, useNavigate: () => mockNavigate };
});

vi.mock('@/modules/auth/hook/auth.hook');

const mockedUseAuth = vi.mocked(useAuth);

describe('SidebarLayout — HU-03 logout redirect', () => {
  const logOut = vi.fn().mockResolvedValue(undefined);

  beforeEach(() => {
    mockNavigate.mockClear();
    logOut.mockClear();
    mockedUseAuth.mockReturnValue({
      role: UserRolesType.Owner,
      logOut,
      signIn: vi.fn(),
      isAuthenticated: true,
      user: null,
    } as unknown as ReturnType<typeof useAuth>);
  });

  it('signs out and redirects to the public home', async () => {
    render(
      <MemoryRouter initialEntries={['/panel/torneos']}>
        <SidebarLayout>
          <div>panel content</div>
        </SidebarLayout>
      </MemoryRouter>
    );

    fireEvent.click(screen.getByText('Cerrar sesión'));

    await waitFor(() => expect(logOut).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/'));
  });
});

describe('SidebarLayout — HU-26/HU-27 slimmed navigation', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    mockedUseAuth.mockReturnValue({
      role: UserRolesType.Owner,
      logOut: vi.fn().mockResolvedValue(undefined),
      signIn: vi.fn(),
      isAuthenticated: true,
      user: null,
    } as unknown as ReturnType<typeof useAuth>);
  });

  const renderSidebar = () =>
    render(
      <MemoryRouter initialEntries={['/panel/torneos']}>
        <SidebarLayout>
          <div>panel content</div>
        </SidebarLayout>
      </MemoryRouter>
    );

  it('no longer lists Divisiones, Fases, Partidos or the tournament wizard', () => {
    renderSidebar();

    expect(screen.queryByText('Divisiones')).not.toBeInTheDocument();
    expect(screen.queryByText('Fases')).not.toBeInTheDocument();
    expect(screen.queryByText('Partidos')).not.toBeInTheDocument();
    expect(screen.queryByText('Asistente de torneo')).not.toBeInTheDocument();
  });

  it('keeps the tournament-management and admin entries', () => {
    renderSidebar();

    ['Torneos', 'Sanciones', 'Canchas', 'Equipos', 'Usuarios', 'Novedades'].forEach(
      label => expect(screen.getByText(label)).toBeInTheDocument()
    );
  });
});
