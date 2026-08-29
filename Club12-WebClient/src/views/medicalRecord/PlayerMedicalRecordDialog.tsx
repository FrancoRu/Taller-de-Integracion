import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import { useMedicalRecord } from '@/modules/medicalRecord/hook/medicalRecord.hook';
import { medicalRecordService } from '@/modules/medicalRecord/service/medicalRecord.service';
import { IMedicalRecordResponse } from '@/modules/medicalRecord/type/medicalRecord.d';
import { isStoredMedicalRecordFile } from '@/modules/medicalRecord/utils/medicalRecordDisplay';
import {
  notifyError,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { DownloadIcon, UploadFileIcon } from '@/views/core/MUI/icons/icons';
import HabilitacionBadge from '@/views/medicalRecord/HabilitacionBadge';

interface PlayerMedicalRecordDialogProps {
  open: boolean;
  onClose: () => void;
  playerId: GUID;
  teamId: GUID;
  tournamentId: GUID;
  playerName: string;
  /** Initial status from the roster, refined once the record is fetched. */
  status?: MedicalRecordStatus | null;
  /** Initial eligibility from the roster (HU-57). */
  isHabilitado?: boolean | null;
  /** Called after a successful upload or review so the roster can refresh. */
  onChanged?: () => void;
}

const isPdf = (file: File): boolean =>
  file.type === 'application/pdf' || file.name.toLowerCase().endsWith('.pdf');

/**
 * Owner/admin dialog to manage a player's medical record within a specific
 * team and tournament: upload the PDF (HU-55) and approve/reject it with an
 * optional reason (HU-58). The resulting habilitación is reflected by the
 * badge (HU-57).
 */
const PlayerMedicalRecordDialog: React.FC<PlayerMedicalRecordDialogProps> = ({
  open,
  onClose,
  playerId,
  teamId,
  tournamentId,
  playerName,
  status,
  isHabilitado,
  onChanged,
}) => {
  const { uploadMedicalRecord, reviewMedicalRecord } = useMedicalRecord();
  const [record, setRecord] = useState<IMedicalRecordResponse | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [rejectReason, setRejectReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const effectiveStatus = record?.status ?? status ?? null;
  const effectiveHabilitado = record?.isHabilitado ?? isHabilitado ?? false;
  // Once the ficha is Approved (habilitado) it is frozen: the stored file can
  // only be viewed/downloaded, never replaced (HU-57).
  const isApproved = effectiveStatus === MedicalRecordStatus.Approved;
  const hasStoredFile = Boolean(record?.fileUrl ?? record?.fileName);
  // Approving requires a real (non-legacy) stored file reference — the
  // backend rejects an approve with no file (medical-records-storage-eligibility
  // Part 2), so the UI disables the action up front instead of surfacing a 409.
  const canApprove = isStoredMedicalRecordFile(record?.fileUrl);

  const handleDownload = async () => {
    try {
      await medicalRecordService.downloadMedicalRecord(
        playerId,
        teamId,
        tournamentId,
        record?.fileName ?? 'ficha-medica.pdf'
      );
    } catch {
      void notifyError({
        title: 'No se pudo descargar',
        text: 'No se pudo descargar la ficha médica. Intentá nuevamente.',
      });
    }
  };

  useEffect(() => {
    if (!open) {
      setRecord(null);
      setSelectedFile(null);
      setRejectReason('');
      return;
    }

    let active = true;
    // The record legitimately does not exist yet (404) before the first
    // upload, so a lookup failure here is treated as "no record" rather than
    // surfaced as a global error.
    void medicalRecordService
      .getMedicalRecord(playerId, teamId, tournamentId)
      .then(res => {
        if (active) {
          setRecord(res.data);
          setRejectReason(res.data.reviewReason ?? '');
        }
      })
      .catch(() => {
        if (active) {
          setRecord(null);
        }
      });

    return () => {
      active = false;
    };
  }, [open, playerId, teamId, tournamentId]);

  const handleUpload = useCallback(async () => {
    if (!selectedFile) {
      void notifyWarning({
        title: 'Archivo requerido',
        text: 'Seleccione un archivo PDF de ficha médica.',
      });
      return;
    }

    if (!isPdf(selectedFile)) {
      void notifyWarning({
        title: 'Formato inválido',
        text: 'La ficha médica debe ser un archivo PDF.',
      });
      return;
    }

    setSubmitting(true);
    const result = await uploadMedicalRecord({
      playerId,
      teamId,
      tournamentId,
      file: selectedFile,
    });
    setSubmitting(false);

    if (!result) {
      return;
    }

    setRecord(result);
    setSelectedFile(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    onChanged?.();
    await notifySuccess({
      title: 'Ficha médica cargada',
      text: 'La ficha quedó pendiente de aprobación.',
    });
  }, [
    onChanged,
    playerId,
    selectedFile,
    teamId,
    tournamentId,
    uploadMedicalRecord,
  ]);

  const handleReview = useCallback(
    async (approve: boolean) => {
      setSubmitting(true);
      const result = await reviewMedicalRecord({
        playerId,
        teamId,
        tournamentId,
        approve,
        reason: approve ? undefined : rejectReason.trim() || undefined,
      });
      setSubmitting(false);

      if (!result) {
        return;
      }

      setRecord(result);
      onChanged?.();
      await notifySuccess({
        title: approve ? 'Ficha aprobada' : 'Ficha rechazada',
        text: approve
          ? 'El jugador quedó habilitado.'
          : 'El jugador quedó no habilitado.',
      });
    },
    [onChanged, playerId, rejectReason, reviewMedicalRecord, teamId, tournamentId]
  );

  return (
    <Dialog
      open={open}
      onClose={() => !submitting && onClose()}
      fullWidth
      maxWidth="sm"
    >
      <DialogTitle>Ficha médica — {playerName}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
              Estado
            </Typography>
            <HabilitacionBadge
              isHabilitado={effectiveHabilitado}
              status={effectiveStatus}
            />
          </Stack>

          {hasStoredFile && (
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{ alignItems: { sm: 'center' } }}
            >
              <Typography variant="body2">
                Archivo cargado: {record?.fileName ?? 'ficha-medica.pdf'}
              </Typography>
              <Button
                variant="outlined"
                size="small"
                startIcon={<DownloadIcon fontSize="small" />}
                onClick={() => void handleDownload()}
              >
                Descargar ficha
              </Button>
            </Stack>
          )}

          {effectiveStatus === MedicalRecordStatus.Rejected &&
            record?.reviewReason && (
              <Typography variant="body2" color="error">
                Motivo de rechazo: {record.reviewReason}
              </Typography>
            )}

          {isApproved && (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              La ficha ya está aprobada y el jugador quedó habilitado. Solo puede
              consultarse o descargarse.
            </Typography>
          )}

          {!isApproved && (
            <>
              <Divider />

              <Typography variant="subtitle2">
                Subir ficha médica (PDF)
              </Typography>
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={1}
                sx={{ alignItems: { sm: 'center' } }}
              >
                <Button
                  variant="outlined"
                  component="label"
                  startIcon={<UploadFileIcon fontSize="small" />}
                  disabled={submitting}
                >
                  Seleccionar PDF
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="application/pdf,.pdf"
                    hidden
                    onChange={e => setSelectedFile(e.target.files?.[0] ?? null)}
                  />
                </Button>
                <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                  {selectedFile?.name ?? 'Ningún archivo seleccionado'}
                </Typography>
                <Button
                  variant="contained"
                  onClick={() => void handleUpload()}
                  disabled={submitting || !selectedFile}
                >
                  Subir ficha
                </Button>
              </Stack>
            </>
          )}

          {!isApproved && (
            <>
              <Divider />

              <Typography variant="subtitle2">
                Revisión (aprobar / rechazar)
              </Typography>
              <TextField
                label="Motivo de rechazo (opcional)"
                value={rejectReason}
                onChange={e => setRejectReason(e.target.value)}
                fullWidth
                multiline
                minRows={2}
                slotProps={{ htmlInput: { maxLength: 500 } }}
              />
              <Stack direction="row" spacing={1}>
                <Tooltip
                  title={
                    canApprove ? '' : 'Subí la ficha médica antes de aprobarla.'
                  }
                >
                  <span>
                    <Button
                      variant="contained"
                      color="success"
                      onClick={() => void handleReview(true)}
                      disabled={submitting || !canApprove}
                    >
                      Aprobar
                    </Button>
                  </span>
                </Tooltip>
                <Button
                  variant="contained"
                  color="error"
                  onClick={() => void handleReview(false)}
                  disabled={submitting}
                >
                  Rechazar
                </Button>
              </Stack>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={submitting} color="inherit">
          Cerrar
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default PlayerMedicalRecordDialog;
