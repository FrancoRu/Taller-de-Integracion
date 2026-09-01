import {
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from '@testing-library/react';
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
    downloadMedicalRecord: vi.fn(),
  },
}));
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn().mockResolvedValue({ isConfirmed: true }) },
}));

const mockedUseMedicalRecord = vi.mocked(useMedicalRecord);
const mockedGet = vi.mocked(medicalRecordService.getMedicalRecord);
const mockedDownload = vi.mocked(medicalRecordService.downloadMedicalRecord);

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
  mockedDownload.mockResolvedValue(undefined);
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

  it('rejects an empty (0-byte) PDF before uploading', async () => {
    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');
    const emptyFile = new File([], 'ficha.pdf', { type: 'application/pdf' });
    const fileInput = dialog.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    await user.upload(fileInput, emptyFile);

    await user.click(
      within(dialog).getByRole('button', { name: /subir ficha/i })
    );

    expect(uploadMedicalRecord).not.toHaveBeenCalled();
  });

  it('rejects a non-PDF file before uploading', async () => {
    // A real <input accept="application/pdf,.pdf"> — and userEvent.upload,
    // which honors it — would normally keep a non-matching file from ever
    // being selected. This exercises the component's OWN isPdf() guard as
    // defense-in-depth (e.g. drag-and-drop bypasses `accept`), so it sets
    // the file directly via fireEvent instead of the accept-aware upload().
    renderDialog();

    const dialog = screen.getByRole('dialog');
    const wordFile = new File(['contenido'], 'ficha.docx', {
      type: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    });
    const fileInput = dialog.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    fireEvent.change(fileInput, { target: { files: [wordFile] } });

    fireEvent.click(
      within(dialog).getByRole('button', { name: /subir ficha/i })
    );

    expect(uploadMedicalRecord).not.toHaveBeenCalled();
  });

  it('approves the record via the review endpoint once a file is stored', async () => {
    // Approving requires a real stored file (medical-records-storage-eligibility
    // Part 2 FE gate) — mock the fetched record with a new-scheme reference so
    // "Aprobar" is enabled.
    mockedGet.mockResolvedValue({
      data: {
        playerId,
        teamId,
        tournamentId,
        status: MedicalRecordStatus.Pending,
        isHabilitado: false,
        fileUrl: `${teamId}/${playerId}/${'a'.repeat(8)}-guid.pdf`,
        fileName: 'ficha.pdf',
      },
    } as Awaited<ReturnType<typeof medicalRecordService.getMedicalRecord>>);

    const user = userEvent.setup();
    renderDialog();

    const dialog = screen.getByRole('dialog');
    await waitFor(() =>
      expect(
        within(dialog).getByRole('button', { name: /aprobar/i })
      ).toBeEnabled()
    );
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

  it('disables "Aprobar" with a tooltip when there is no stored file, but never disables "Rechazar"', async () => {
    // Default setup: getMedicalRecord rejects (404) -> no record, no file.
    renderDialog();

    const dialog = screen.getByRole('dialog');
    const approveButton = within(dialog).getByRole('button', {
      name: /aprobar/i,
    });
    const rejectButton = within(dialog).getByRole('button', {
      name: /rechazar/i,
    });

    expect(approveButton).toBeDisabled();
    expect(rejectButton).toBeEnabled();

    // MUI wraps a disabled button in a non-disabled <span> so the Tooltip can
    // still receive pointer events; hover that wrapper, not the button itself.
    // The tooltip content renders in a Popper portal outside the dialog DOM,
    // so it is looked up via the top-level `screen`, not `within(dialog)`.
    const user = userEvent.setup();
    await user.hover(approveButton.parentElement as HTMLElement);
    expect(
      await screen.findByText(/subí la ficha médica antes de aprobarla/i)
    ).toBeInTheDocument();
  });

  it('disables "Aprobar" when the stored reference is a legacy medical-records/ ref', async () => {
    mockedGet.mockResolvedValue({
      data: {
        playerId,
        teamId,
        tournamentId,
        status: MedicalRecordStatus.Pending,
        isHabilitado: false,
        fileUrl: 'medical-records/some/object/path.pdf',
        fileName: 'ficha.pdf',
      },
    } as Awaited<ReturnType<typeof medicalRecordService.getMedicalRecord>>);

    renderDialog();

    const dialog = screen.getByRole('dialog');
    await waitFor(() =>
      expect(
        within(dialog).getByRole('button', { name: /aprobar/i })
      ).toBeDisabled()
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

  it('hides upload and downloads the stored ficha when the record is already approved', async () => {
    // An approved (habilitado) record: only view/download must be offered —
    // uploading a new ficha is forbidden (HU-57).
    mockedGet.mockResolvedValue({
      data: {
        playerId,
        teamId,
        tournamentId,
        status: MedicalRecordStatus.Approved,
        isHabilitado: true,
        fileUrl: 'medical-records/some/object/path.pdf',
        fileName: 'ficha.pdf',
      },
    } as Awaited<ReturnType<typeof medicalRecordService.getMedicalRecord>>);

    const user = userEvent.setup();
    render(
      <PlayerMedicalRecordDialog
        open
        onClose={vi.fn()}
        playerId={playerId}
        teamId={teamId}
        tournamentId={tournamentId}
        playerName="Juan Pérez"
        status={MedicalRecordStatus.Approved}
        isHabilitado
      />
    );

    const dialog = screen.getByRole('dialog');

    await waitFor(() =>
      expect(within(dialog).getByText('Habilitado')).toBeInTheDocument()
    );

    // Upload / review controls are gone; only the download stays.
    expect(
      within(dialog).queryByRole('button', { name: /subir ficha/i })
    ).not.toBeInTheDocument();
    expect(
      within(dialog).queryByRole('button', { name: /aprobar/i })
    ).not.toBeInTheDocument();

    await user.click(
      within(dialog).getByRole('button', { name: /descargar ficha/i })
    );

    expect(mockedDownload).toHaveBeenCalledWith(
      playerId,
      teamId,
      tournamentId,
      'ficha.pdf'
    );
  });
});
