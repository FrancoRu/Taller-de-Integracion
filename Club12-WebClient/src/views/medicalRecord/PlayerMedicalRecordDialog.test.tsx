import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Mock } from 'vitest';
import PlayerMedicalRecordDialog from '@/views/medicalRecord/PlayerMedicalRecordDialog';
import { useMedicalRecord } from '@/modules/medicalRecord/hook/medicalRecord.hook';
import { medicalRecordService } from '@/modules/medicalRecord/service/medicalRecord.service';
import type { IMedicalRecordContextProps } from '@/modules/medicalRecord/type/medicalRecord.d';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/medicalRecord/hook/medicalRecord.hook');
vi.mock('@/modules/medicalRecord/service/medicalRecord.service', () => ({
  medicalRecordService: {
    getMedicalRecord: vi.fn(),
    uploadMedicalRecord: vi.fn(),
    reviewMedicalRecord: vi.fn(),
  },
}));
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn().mockResolvedValue({ isConfirmed: true }) },
}));

const mockedUseMedicalRecord = vi.mocked(useMedicalRecord);
const mockedGet = vi.mocked(medicalRecordService.getMedicalRecord);

const playerId = '11111111-1111-1111-1111-111111111111' as GUID;
const teamId = '22222222-2222-2222-2222-222222222222' as GUID;
const tournamentId = '33333333-3333-3333-3333-333333333333' as GUID;

let uploadMedicalRecord: Mock<IMedicalRecordContextProps['uploadMedicalRecord']>;
let reviewMedicalRecord: Mock<IMedicalRecordContextProps['reviewMedicalRecord']>;

const setup = () => {
  uploadMedicalRecord = vi.fn<IMedicalRecordContextProps['uploadMedicalRecord']>();
  uploadMedicalRecord.mockResolvedValue({
    playerId,
    teamId,
    tournamentId,
    status: MedicalRecordStatus.Pending,
    isHabilitado: false,
    fileName: 'ficha.pdf',
  });
  reviewMedicalRecord = vi.fn<IMedicalRecordContextProps['reviewMedicalRecord']>();
  reviewMedicalRecord.mockResolvedValue({
    playerId,
    teamId,
    tournamentId,
    status: MedicalRecordStatus.Approved,
    isHabilitado: true,
  });

  mockedUseMedicalRecord.mockReturnValue({
    uploadMedicalRecord,
    reviewMedicalRecord,
    getMedicalRecord: vi.fn(),
  });
  // No record exists yet on open.
  mockedGet.mockRejectedValue(new Error('404'));
};

const renderDialog = () =>
  render(
    <PlayerMedicalRecordDialog
      open
      onClose={vi.fn()}
      playerId={playerId}
      teamId={teamId}
      tournamentId={tournamentId}
      playerName="Juan Pérez"
      status={MedicalRecordStatus.Pending}
      isHabilitado={false}
    />
  );

beforeEach(() => {
  vi.clearAllMocks();
  setup();
});

describe('PlayerMedicalRecordDialog', () => {
  it('shows the not-habilitado state and uploads the selected PDF via FormData', async () => {
    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByText('No habilitado')).toBeInTheDocument();

    const file = new File(['%PDF-1.4'], 'ficha.pdf', {
      type: 'application/pdf',
    });
    const fileInput = dialog.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    await user.upload(fileInput, file);

    await user.click(within(dialog).getByRole('button', { name: /subir ficha/i }));

    await waitFor(() => expect(uploadMedicalRecord).toHaveBeenCalledTimes(1));
    expect(uploadMedicalRecord).toHaveBeenCalledWith({
      playerId,
      teamId,
      tournamentId,
      file,
    });
  });

  it('approves the record via the review endpoint', async () => {
    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: /aprobar/i }));

    await waitFor(() => expect(reviewMedicalRecord).toHaveBeenCalledTimes(1));
    expect(reviewMedicalRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        playerId,
        teamId,
        tournamentId,
        approve: true,
      })
    );
  });

  it('rejects the record with the typed reason', async () => {
    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');
    await user.type(
      within(dialog).getByRole('textbox', { name: /motivo de rechazo/i }),
      'Faltan firmas'
    );
    await user.click(within(dialog).getByRole('button', { name: /rechazar/i }));

    await waitFor(() => expect(reviewMedicalRecord).toHaveBeenCalledTimes(1));
    expect(reviewMedicalRecord).toHaveBeenCalledWith(
      expect.objectContaining({
        approve: false,
        reason: 'Faltan firmas',
      })
    );
  });
});
