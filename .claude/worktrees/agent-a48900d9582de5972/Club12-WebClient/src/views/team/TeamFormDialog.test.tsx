import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import TeamFormDialog from '@/views/team/TeamFormDialog';
import type { TeamFormState } from '@/views/team/teams.types';

const emptyForm: TeamFormState = {
  name: '',
  threeLetterCode: '',
  shirtColor: '',
  logo: null,
};

describe('TeamFormDialog', () => {
  it('renders the logo picker and fires onLogoChange when withLogo is true', async () => {
    const onLogoChange = vi.fn();
    const user = userEvent.setup();

    render(
      <TeamFormDialog
        open
        title="Nuevo equipo"
        confirmLabel="Crear"
        withLogo
        form={emptyForm}
        submitting={false}
        onFieldChange={vi.fn()}
        onLogoChange={onLogoChange}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
      />
    );

    const dialog = screen.getByRole('dialog');
    expect(
      within(dialog).getByRole('button', { name: /seleccionar logo/i })
    ).toBeInTheDocument();

    const file = new File(['logo'], 'logo.png', { type: 'image/png' });
    const fileInput = dialog.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    await user.upload(fileInput, file);

    expect(onLogoChange).toHaveBeenCalledWith(file);
  });

  it('omits the logo picker when withLogo is false', () => {
    render(
      <TeamFormDialog
        open
        title="Editar equipo"
        confirmLabel="Guardar"
        withLogo={false}
        form={emptyForm}
        submitting={false}
        onFieldChange={vi.fn()}
        onLogoChange={vi.fn()}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
      />
    );

    const dialog = screen.getByRole('dialog');
    expect(
      within(dialog).queryByRole('button', { name: /seleccionar logo/i })
    ).not.toBeInTheDocument();
  });

  it('fires onFieldChange with the field key and raw value, and wires onConfirm/onClose', () => {
    const onFieldChange = vi.fn();
    const onConfirm = vi.fn();
    const onClose = vi.fn();

    render(
      <TeamFormDialog
        open
        title="Nuevo equipo"
        confirmLabel="Crear"
        withLogo
        form={emptyForm}
        submitting={false}
        onFieldChange={onFieldChange}
        onLogoChange={vi.fn()}
        onClose={onClose}
        onConfirm={onConfirm}
      />
    );

    const dialog = screen.getByRole('dialog');
    fireEvent.change(
      within(dialog).getByRole('textbox', { name: /^Código/ }),
      { target: { value: 'riv' } }
    );
    expect(onFieldChange).toHaveBeenCalledWith('threeLetterCode', 'riv');

    fireEvent.click(within(dialog).getByRole('button', { name: /crear/i }));
    expect(onConfirm).toHaveBeenCalledTimes(1);

    fireEvent.click(
      within(dialog).getByRole('button', { name: /cancelar/i })
    );
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
