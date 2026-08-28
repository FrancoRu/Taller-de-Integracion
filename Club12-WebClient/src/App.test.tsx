import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import App from '@/App';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

vi.mock('@/modules/auth/hook/auth.hook', () => ({
  useAuth: () => ({
    isAuthenticated: false,
    role: UserRolesType.Guest,
    signIn: vi.fn(),
    logOut: vi.fn(),
    user: null,
  }),
}));

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>
  );

describe('App public layout chrome', () => {
  it('HU-02: renders /login without header or footer', () => {
    renderAt('/login');

    expect(screen.getByText('Administrador')).toBeInTheDocument();
    expect(document.querySelector('header')).toBeNull();
    expect(document.querySelector('footer')).toBeNull();
  });

  it('HU-04: renders the 404 page without header or footer', () => {
    renderAt('/una-ruta-que-no-existe');

    expect(
      screen.getByText(/no existe o fue movida/i)
    ).toBeInTheDocument();
    expect(document.querySelector('header')).toBeNull();
    expect(document.querySelector('footer')).toBeNull();
  });

  it('keeps header and footer on a normal public route', () => {
    renderAt('/quienes-somos');

    expect(document.querySelector('header')).not.toBeNull();
    expect(document.querySelector('footer')).not.toBeNull();
  });
});
