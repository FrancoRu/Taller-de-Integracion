import { useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { DeleteIcon } from '@/views/core/MUI/icons/icons';
import { GUID } from '@/modules/core/types/types';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { usePointDeductions } from '@/modules/pointDeduction/hook/pointDeduction.hook';

/** The maximum length the backend accepts for a deduction reason. */
const REASON_MAX_LENGTH = 300;

interface PointDeductionManagerProps {
  /** The division whose deductions are being managed. */
  divisionId: GUID;
  /** The teams that can be penalised (those in the division's standings). */
  teams: { id: GUID; name: string }[];
}

/**
 * Admin panel (AdminOrOwner) to apply and remove disciplinary point deductions
 * (deducción de puntos) for the teams of a division. The subtraction is
 * reflected in the public standings.
 */
const PointDeductionManager: React.FC<PointDeductionManagerProps> = ({
  divisionId,
  teams,
}) => {
  const { deductions, loading, create, remove } =
    usePointDeductions(divisionId);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [teamId, setTeamId] = useState<GUID | ''>('');
  const [points, setPoints] = useState('1');
  const [reason, setReason] = useState('');

  const resetForm = () => {
    setTeamId('');
    setPoints('1');
    setReason('');
  };

  const openDialog = () => {
    resetForm();
    setDialogOpen(true);
  };

  const closeDialog = () => {
    if (!submitting) {
      setDialogOpen(false);
    }
  };

  const submit = async () => {
    const parsedPoints = Number(points);

    if (!teamId) {
      await notifyWarning({ title: 'Elegí un equipo para la deducción.' });
      return;
    }
    if (!Number.isInteger(parsedPoints) || parsedPoints < 1) {
      await notifyWarning({
        title: 'Los puntos a descontar deben ser un número entero de al menos 1.',
      });
      return;
    }
    if (!reason.trim()) {
      await notifyWarning({ title: 'Ingresá el motivo de la deducción.' });
      return;
    }

    setSubmitting(true);
    try {
      await create({ teamId, points: parsedPoints, reason: reason.trim() });
      setDialogOpen(false);
      resetForm();
      await notifySuccess({ title: 'Deducción aplicada correctamente.' });
    } catch {
      await notifyError({
        title: 'No se pudo aplicar la deducción.',
        text: 'Intentá nuevamente en unos segundos.',
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: GUID, teamName?: string) => {
    const confirmed = await confirmDelete({
      title: 'Quitar deducción',
      text: `¿Seguro que querés quitar la deducción${
        teamName ? ` de ${teamName}` : ''
      }? Los puntos se restablecerán en la tabla.`,
    });
    if (!confirmed) {
      return;
    }

    try {
      await remove(id);
      await notifySuccess({ title: 'Deducción eliminada.' });
    } catch {
      await notifyError({ title: 'No se pudo eliminar la deducción.' });
    }
  };

  return (
    <Box>
      <Stack
        direction="row"
        sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}
      >
        <Typography variant="subtitle1" component="h3">
          Deducción de puntos
        </Typography>
        <Button variant="contained" size="small" onClick={openDialog}>
          Agregar deducción
        </Button>
      </Stack>

      {deductions.length === 0 ? (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {loading
            ? 'Cargando deducciones…'
            : 'Esta división no tiene deducciones de puntos.'}
        </Typography>
      ) : (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Equipo</TableCell>
                <TableCell align="center">Puntos</TableCell>
                <TableCell>Motivo</TableCell>
                <TableCell align="center">Quitar</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {deductions.map(deduction => (
                <TableRow key={deduction.id} hover>
                  <TableCell>{deduction.teamName ?? '—'}</TableCell>
                  <TableCell align="center" sx={{ color: 'error.main', fontWeight: 600 }}>
                    -{deduction.points}
                  </TableCell>
                  <TableCell>{deduction.reason}</TableCell>
                  <TableCell align="center">
                    <Tooltip title="Quitar deducción">
                      <IconButton
                        size="small"
                        color="error"
                        aria-label={`Quitar deducción de ${
                          deduction.teamName ?? 'equipo'
                        }`}
                        onClick={() =>
                          void handleDelete(deduction.id, deduction.teamName)
                        }
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}

      <Dialog open={dialogOpen} onClose={closeDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Nueva deducción de puntos</DialogTitle>
        <DialogContent>
          <TextField
            select
            label="Equipo"
            value={teamId}
            onChange={e => setTeamId(e.target.value as GUID)}
            fullWidth
            sx={{ mt: 1 }}
            disabled={teams.length === 0}
            helperText={
              teams.length === 0
                ? 'Todavía no hay equipos en la tabla de esta división.'
                : undefined
            }
          >
            {teams.map(team => (
              <MenuItem key={team.id} value={team.id}>
                {team.name}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label="Puntos a descontar"
            type="number"
            value={points}
            onChange={e => setPoints(e.target.value)}
            fullWidth
            slotProps={{ htmlInput: { min: 1, step: 1 } }}
            sx={{ mt: 2 }}
          />

          <TextField
            label="Motivo"
            value={reason}
            onChange={e => setReason(e.target.value)}
            multiline
            minRows={3}
            fullWidth
            sx={{ mt: 2 }}
            slotProps={{ htmlInput: { maxLength: REASON_MAX_LENGTH } }}
            helperText={`${reason.length}/${REASON_MAX_LENGTH}`}
          />

          <Stack
            direction="row"
            spacing={1}
            sx={{ justifyContent: 'flex-end', mt: 2 }}
          >
            <Button color="inherit" onClick={closeDialog} disabled={submitting}>
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void submit()}
              disabled={submitting || teams.length === 0}
            >
              Aplicar deducción
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default PointDeductionManager;
