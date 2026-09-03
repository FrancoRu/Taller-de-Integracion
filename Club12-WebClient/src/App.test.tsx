import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import App from '@/App';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

// Mutable so a test can flip the session to authenticated. Hoisted because the
// vi.mock factory below runs before module imports.
const authState = vi.hoisted(() => ({
  isAuthenticated: false,
  role: 'Guest' as string,
}));

vi.mock('@/modules/auth/hook/auth.hook', () => ({
  useAuth: () => ({
    isAuthenticated: authState.isAuthenticated,
    role: authState.role,
    signIn: vi.fn(),
    logOut: vi.fn(),
    user: null,
  }),
}));

afterEach(() => {
  authState.isAuthenticated = false;
  authState.role = 'Guest';
});

const renderAt = (path: string) =>
  render(
    <MemoryRouter initialEntries={[path]}>
      <App />
    </MemoryRouter>
  );

describe('App public layout chrome', () => {
  it('HU-02: renders /login without header or footer', async () => {
    renderAt('/login');

    // Route-level pages are React.lazy-loaded (see App.tsx), so the chunk
    // resolves asynchronously behind a Suspense fallback — a synchronous
    // getByText would race that and fail before the real content mounts.
    expect(await screen.findByText('Administrador')).toBeInTheDocument();
    expect(document.querySelector('header')).toBeNull();
    expect(document.querySelector('footer')).toBeNull();
  });

  it('HU-04: renders the 404 page without header or footer', async () => {
    renderAt('/una-ruta-que-no-existe');

    expect(
      await screen.findByText(/no existe o fue movida/i)
    ).toBeInTheDocument();
    expect(document.querySelector('header')).toBeNull();
    expect(document.querySelector('footer')).toBeNull();
  });

  it('keeps header and footer on a normal public route', async () => {
    renderAt('/quienes-somos');

    expect(await screen.findByRole('contentinfo')).toBeInTheDocument();
    expect(document.querySelector('header')).not.toBeNull();
  });

  it('lets an authenticated admin open a public page instead of 404', async () => {
    // Regression: public slug routes (tournament/blog/team/match) used to be
    // omitted entirely for authenticated users, so any public URL 404'd from
    // the panel catch-all without ever hitting the API. They must resolve for
    // logged-in users too, under the public layout (not the admin sidebar).
    authState.isAuthenticated = true;
    authState.role = UserRolesType.Admin;

    renderAt('/quienes-somos');

    expect(await screen.findByRole('contentinfo')).toBeInTheDocument();
    expect(screen.queryByText(/no existe o fue movida/i)).toBeNull();
    expect(document.querySelector('header')).not.toBeNull();
  });
});
