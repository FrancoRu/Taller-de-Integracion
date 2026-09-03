import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import EditUser from '@/views/user/editUser';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

const USER_ID = '11111111-1111-1111-1111-111111111111';

const getById = vi.fn().mockResolvedValue({
  id: USER_ID,
  username: 'seed-tm-857444',
  email: 'seed-teammanager-857444@club12.test',
  phoneNumber: '3435551234',
  role: UserRolesType.Admin,
});
const updateUser = vi.fn().mockResolvedValue({ id: USER_ID });

vi.mock('@/modules/auth/hook/auth.hook', () => ({
  useAuth: () => ({ role: UserRolesType.Admin }),
}));
vi.mock('@/modules/user/hook/user.hook', () => ({
  useUser: () => ({ user: { id: USER_ID }, getById, updateUser }),
}));
vi.mock('@/modules/error/hooks/error.hock', () => ({
  useError: () => ({ errors: [], setMessage: vi.fn() }),
}));

const renderForm = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/usuarios/${USER_ID}/editar`]}>
      <Routes>
        <Route path="/panel/usuarios/:userId/editar" element={<EditUser />} />
      </Routes>
    </MemoryRouter>
  );

describe('EditUser — clearing an optional field', () => {
  it('sends an empty phone as "" (clear it), not omitted from the payload', async () => {
    // Regression: the backend already treats an empty string as "clear this
    // field" (request.Phone is not null, IdentityUserManagementService) —
    // but the form used to build phone via `value?.trim() || undefined`,
    // the same guard used for the REQUIRED username/email fields. For
    // phone (optional, clearable) that meant an emptied field was silently
    // dropped from the payload instead of clearing it: no error, "Guardar
    // cambios" reported success, and the old phone number just never went
    // away — found by hand-testing this exact flow on staging.
    const user = userEvent.setup();
    renderForm();

    const phoneField = await screen.findByRole('textbox', { name: 'Teléfono' });
    expect(phoneField).toHaveValue('3435551234');

    await user.clear(phoneField);
    await user.click(screen.getByRole('button', { name: 'Guardar cambios' }));

    await waitFor(() => expect(updateUser).toHaveBeenCalled());
    expect(updateUser).toHaveBeenCalledWith(
      USER_ID,
      expect.objectContaining({ phone: '' })
    );
  });
});
