import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import CreateUser from '@/views/user/createUser';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

// HU-05: only Owner and Admin remain assignable. These mocks give the form a
// super-admin (Owner) caller so the role selector renders with the full list
// of roles it is willing to offer.
vi.mock('@/modules/auth/hook/auth.hook', () => ({
  useAuth: () => ({ role: UserRolesType.Owner }),
}));
vi.mock('@/modules/user/hook/user.hook', () => ({
  useUser: () => ({ createUser: vi.fn() }),
}));
vi.mock('@/modules/error/hooks/error.hock', () => ({
  useError: () => ({ errors: [], setMessage: vi.fn() }),
}));

const renderForm = () =>
  render(
    <MemoryRouter>
      <CreateUser />
    </MemoryRouter>
  );

describe('CreateUser role selector (HU-05)', () => {
  it('offers only Admin and Owner — never Tournament/Team Manager', async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole('combobox', { name: /rol/i }));

    const optionLabels = screen
      .getAllByRole('option')
      .map(option => option.textContent);

    expect(optionLabels).toEqual(
      expect.arrayContaining(['Admin', 'Owner'])
    );
    expect(optionLabels).not.toContain('Responsable del Torneo');
    expect(optionLabels).not.toContain('Responsable de Equipo');
  });
});
