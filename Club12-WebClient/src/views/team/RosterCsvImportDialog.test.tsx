import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { AxiosError } from 'axios';
import { afterEach, describe, expect, it, vi } from 'vitest';
import RosterCsvImportDialog from '@/views/team/RosterCsvImportDialog';
import { playerService } from '@/modules/player/service/player.service';
import type { IPlayerResponse } from '@/modules/player/type/player.d';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/player/service/player.service');
vi.mock('@/modules/core/utils/confirmDialog', () => ({
  notifyError: vi.fn(),
  notifySuccess: vi.fn(),
  notifyWarning: vi.fn(),
}));

const mockedAddPlayer = vi.mocked(playerService.addPlayer);

const TEAM_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' as GUID;

const CSV_CONTENT = [
  'Nombre,Segundo nombre,Apellido,Documento,Fecha de nacimiento,Teléfono,Obra social',
  'Ana,,Gómez,30111222,2000-05-05,3511234567,OSDE',
  'Beto,Luis,Ruiz,30333444,1998-02-10,3517654321,Swiss Medical',
  ',,SinNombre,12345678,2000-01-01,3510000000,OSDE', // missing firstName -> invalid
].join('\r\n');

const uploadCsv = (content: string, filename = 'plantel.csv') => {
  const file = new File([content], filename, { type: 'text/csv' });
  const input = document.querySelector(
    'input[type="file"]'
  ) as HTMLInputElement;
  fireEvent.change(input, { target: { files: [file] } });
};

const okResponse = { data: {} as IPlayerResponse } as never;

const conflictError = (detail: string) =>
  new AxiosError('Request failed', undefined, undefined, undefined, {
    status: 409,
    data: { detail },
  } as never);

afterEach(() => {
  vi.clearAllMocks();
});

describe('RosterCsvImportDialog', () => {
  it('parses a CSV file and shows valid/invalid row counts', async () => {
    render(<RosterCsvImportDialog open onClose={vi.fn()} teamId={TEAM_ID} />);

    uploadCsv(CSV_CONTENT);

    expect(
      await screen.findByText(/plantel\.csv · 3 fila\(s\), 2 válida\(s\), 1 con errores\./)
    ).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Importar (2)' })
    ).toBeInTheDocument();
  });

  it('imports only the valid rows and reports completion', async () => {
    mockedAddPlayer.mockResolvedValue(okResponse);
    const onImported = vi.fn();
    const onClose = vi.fn();

    render(
      <RosterCsvImportDialog
        open
        onClose={onClose}
        teamId={TEAM_ID}
        onImported={onImported}
      />
    );

    uploadCsv(CSV_CONTENT);
    await screen.findByRole('button', { name: 'Importar (2)' });

    fireEvent.click(screen.getByRole('button', { name: 'Importar (2)' }));

    await waitFor(() => expect(mockedAddPlayer).toHaveBeenCalledTimes(2));
    expect(mockedAddPlayer).toHaveBeenCalledWith(
      expect.objectContaining({
        firstName: 'Ana',
        lastName: 'Gómez',
        documentNumber: '30111222',
        phoneNumber: '3511234567',
        socialSecurity: 'OSDE',
        teamId: TEAM_ID,
      })
    );
    expect(mockedAddPlayer).toHaveBeenCalledWith(
      expect.objectContaining({
        firstName: 'Beto',
        secondName: 'Luis',
        lastName: 'Ruiz',
        documentNumber: '30333444',
        teamId: TEAM_ID,
      })
    );

    await waitFor(() => expect(onClose).toHaveBeenCalled());
    expect(onImported).toHaveBeenCalled();
  });

  it('disables Importar when there are no valid rows', async () => {
    render(<RosterCsvImportDialog open onClose={vi.fn()} teamId={TEAM_ID} />);

    uploadCsv(
      [
        'Nombre,Segundo nombre,Apellido,Documento,Fecha de nacimiento,Teléfono,Obra social',
        ',,,,,,',
      ].join('\r\n')
    );

    const importButton = await screen.findByRole('button', { name: 'Importar (0)' });
    expect(importButton).toBeDisabled();
  });

  it('shows each row\'s own failure reason instead of a global alert, and keeps the dialog open', async () => {
    mockedAddPlayer
      .mockResolvedValueOnce(okResponse)
      .mockRejectedValueOnce(conflictError('Ya existe un jugador con ese documento.'));
    const onImported = vi.fn();
    const onClose = vi.fn();

    render(
      <RosterCsvImportDialog
        open
        onClose={onClose}
        teamId={TEAM_ID}
        onImported={onImported}
      />
    );

    uploadCsv(CSV_CONTENT);
    await screen.findByRole('button', { name: 'Importar (2)' });
    fireEvent.click(screen.getByRole('button', { name: 'Importar (2)' }));

    expect(
      await screen.findByText('Ya existe un jugador con ese documento.')
    ).toBeInTheDocument();
    expect(await screen.findByText('Importado')).toBeInTheDocument();

    // The dialog stays open so the failure is readable, but the roster still
    // refreshes since one row DID get created.
    expect(onClose).not.toHaveBeenCalled();
    expect(onImported).toHaveBeenCalled();
  });

  it('retrying after a partial failure only re-submits the still-pending row', async () => {
    mockedAddPlayer
      .mockResolvedValueOnce(okResponse)
      .mockRejectedValueOnce(conflictError('Ya existe un jugador con ese documento.'))
      .mockResolvedValueOnce(okResponse);

    render(<RosterCsvImportDialog open onClose={vi.fn()} teamId={TEAM_ID} />);

    uploadCsv(CSV_CONTENT);
    await screen.findByRole('button', { name: 'Importar (2)' });
    fireEvent.click(screen.getByRole('button', { name: 'Importar (2)' }));

    await screen.findByRole('button', { name: 'Importar (1)' });
    fireEvent.click(screen.getByRole('button', { name: 'Importar (1)' }));

    await waitFor(() => expect(mockedAddPlayer).toHaveBeenCalledTimes(3));
    // The 3rd call must be for Beto again, not Ana (already imported).
    expect(mockedAddPlayer).toHaveBeenLastCalledWith(
      expect.objectContaining({ firstName: 'Beto' })
    );
  });
});
