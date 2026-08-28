import { useCallback, useEffect, useRef, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import { useMedicalRecord } from '@/modules/medicalRecord/hook/medicalRecord.hook';
import { medicalRecordService } from '@/modules/medicalRecord/service/medicalRecord.service';
import { IMedicalRecordResponse } from '@/modules/medicalRecord/type/medicalRecord.d';
import {
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { UploadFileIcon } from '@/views/core/MUI/icons/icons';
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

          {record?.fileName && (
            <Typography variant="body2">
              Archivo cargado:{' '}
              {record.fileUrl ? (
                <Link
                  href={record.fileUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                >
                  {record.fileName}
                </Link>
              ) : (
                record.fileName
              )}
            </Typography>
          )}

          {effectiveStatus === MedicalRecordStatus.Rejected &&
            record?.reviewReason && (
              <Typography variant="body2" color="error">
                Motivo de rechazo: {record.reviewReason}
              </Typography>
            )}

          <Divider />

          <Typography variant="subtitle2">Subir ficha médica (PDF)</Typography>
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

          <Divider />

          <Typography variant="subtitle2">Revisión (aprobar / rechazar)</Typography>
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
            <Button
              variant="contained"
              color="success"
              onClick={() => void handleReview(true)}
              disabled={submitting}
            >
              Aprobar
            </Button>
            <Button
              variant="contained"
              color="error"
              onClick={() => void handleReview(false)}
              disabled={submitting}
            >
              Rechazar
            </Button>
          </Stack>
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
