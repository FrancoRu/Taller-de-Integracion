import { useState } from 'react';
import {
  Box,
  Button,
  Chip,
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
import { useTeamStaff } from '@/modules/teamStaff/hook/teamStaff.hook';
import { TeamStaffRole } from '@/modules/teamStaff/type/teamStaff';
import { TEAM_STAFF_ROLE_LABEL } from '@/modules/teamStaff/utils/teamStaffDisplay';

/** The role options offered in the add-staff dialog, in display order. */
const ROLE_OPTIONS: TeamStaffRole[] = ['Coach', 'AssistantCoach'];

interface TeamStaffManagerProps {
  /** The team whose technical staff is being managed. */
  teamId: GUID;
  /** The tournament (season participation) to scope the staff to. */
  tournamentId: GUID;
}

/**
 * Admin panel (AdminOrOwner) to add and remove a team's technical staff
 * (cuerpo técnico — DT, Asistente) for a tournament (season)
 * participation.
 */
const TeamStaffManager: React.FC<TeamStaffManagerProps> = ({
  teamId,
  tournamentId,
}) => {
  const { staff, loading, create, remove } = useTeamStaff(
    teamId,
    tournamentId
  );

  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [fullName, setFullName] = useState('');
  const [role, setRole] = useState<TeamStaffRole>('Coach');

  const resetForm = () => {
    setFullName('');
    setRole('Coach');
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
    if (!fullName.trim()) {
      await notifyWarning({ title: 'Ingresá el nombre completo.' });
      return;
    }

    setSubmitting(true);
    try {
      await create({ fullName: fullName.trim(), role, tournamentId });
      setDialogOpen(false);
      resetForm();
      await notifySuccess({ title: 'Cuerpo técnico actualizado correctamente.' });
    } catch {
      await notifyError({
        title: 'No se pudo agregar al cuerpo técnico.',
        text: 'Intentá nuevamente en unos segundos.',
      });
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: GUID, name: string) => {
    const confirmed = await confirmDelete({
      title: 'Quitar del cuerpo técnico',
      text: `¿Seguro que querés quitar a ${name} del cuerpo técnico?`,
    });
    if (!confirmed) {
      return;
    }

    try {
      await remove(id);
      await notifySuccess({ title: 'Miembro del cuerpo técnico eliminado.' });
    } catch {
      await notifyError({ title: 'No se pudo eliminar al miembro del cuerpo técnico.' });
    }
  };

  return (
    <Box>
      <Stack
        direction="row"
        sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}
      >
        <Typography variant="subtitle1" component="h3">
          Cuerpo técnico
        </Typography>
        <Button variant="contained" size="small" onClick={openDialog}>
          Agregar
        </Button>
      </Stack>

      {staff.length === 0 ? (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {loading
            ? 'Cargando cuerpo técnico…'
            : 'Este equipo no tiene cuerpo técnico registrado.'}
        </Typography>
      ) : (
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Nombre</TableCell>
                <TableCell>Rol</TableCell>
                <TableCell align="center">Quitar</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {staff.map(member => (
                <TableRow key={member.id} hover>
                  <TableCell>{member.fullName}</TableCell>
                  <TableCell>
                    <Chip size="small" label={TEAM_STAFF_ROLE_LABEL[member.role]} />
                  </TableCell>
                  <TableCell align="center">
                    <Tooltip title="Quitar del cuerpo técnico">
                      <IconButton
                        size="small"
                        color="error"
                        aria-label={`Quitar del cuerpo técnico a ${member.fullName}`}
                        onClick={() => void handleDelete(member.id, member.fullName)}
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
        <DialogTitle>Agregar al cuerpo técnico</DialogTitle>
        <DialogContent>
          <TextField
            label="Nombre completo"
            value={fullName}
            onChange={e => setFullName(e.target.value)}
            fullWidth
            sx={{ mt: 1 }}
          />

          <TextField
            select
            label="Rol"
            value={role}
            onChange={e => setRole(e.target.value as TeamStaffRole)}
            fullWidth
            sx={{ mt: 2 }}
          >
            {ROLE_OPTIONS.map(option => (
              <MenuItem key={option} value={option}>
                {TEAM_STAFF_ROLE_LABEL[option]}
              </MenuItem>
            ))}
          </TextField>

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
              disabled={submitting}
            >
              Agregar
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Box>
  );
};

export default TeamStaffManager;
