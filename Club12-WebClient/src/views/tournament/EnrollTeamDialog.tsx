import React, { useState } from 'react';
import {
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import FormButtons from '@/views/core/components/FormButtons';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { IEnrollTeamRequest } from '@/modules/tournament/type/tournament';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';

type EnrollMode = 'new' | 'existing';

interface EnrollTeamDialogProps {
  open: boolean;
  submitting: boolean;
  /**
   * Existing teams (clubs) that can be enrolled. The caller is expected to
   * exclude teams already enrolled in this tournament.
   */
  availableTeams: ITeamResponse[];
  onClose: () => void;
  onConfirm: (request: IEnrollTeamRequest) => void;
}

const INITIAL_MODE: EnrollMode = 'new';

/**
 * Dialog to enroll a team into a tournament (HU-107). Two mutually exclusive
 * modes: create a brand-new team by name, or pick an existing team — with an
 * optional "copy roster from previous season" toggle. The resulting request
 * always carries exactly one of `newTeamName` / `existingTeamId`.
 */
const EnrollTeamDialog: React.FC<EnrollTeamDialogProps> = ({
  open,
  submitting,
  availableTeams,
  onClose,
  onConfirm,
}) => {
  const [mode, setMode] = useState<EnrollMode>(INITIAL_MODE);
  const [newTeamName, setNewTeamName] = useState('');
  const [existingTeamId, setExistingTeamId] = useState<GUID | ''>('');
  const [copyRoster, setCopyRoster] = useState(false);

  const reset = () => {
    setMode(INITIAL_MODE);
    setNewTeamName('');
    setExistingTeamId('');
    setCopyRoster(false);
  };

  const handleClose = () => {
    if (submitting) {
      return;
    }
    reset();
    onClose();
  };

  const selectedTeam = availableTeams.find(team => team.id === existingTeamId);

  const handleConfirm = () => {
    if (mode === 'new') {
      const trimmed = newTeamName.trim();
      if (!trimmed) {
        void notifyWarning({
          title: 'Nombre requerido',
          text: 'Ingresá el nombre del nuevo equipo.',
        });
        return;
      }

      onConfirm({ newTeamName: trimmed });
      reset();
      return;
    }

    if (!existingTeamId) {
      void notifyWarning({
        title: 'Equipo requerido',
        text: 'Seleccioná un equipo existente para inscribir.',
      });
      return;
    }

    const request: IEnrollTeamRequest = { existingTeamId };
    if (copyRoster && selectedTeam?.tournamentId) {
      request.copyRosterFromTournamentId = selectedTeam.tournamentId;
    }

    onConfirm(request);
    reset();
  };

  return (
    <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
      <DialogTitle>Inscribir equipo</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <RadioGroup
            row
            value={mode}
            onChange={event => setMode(event.target.value as EnrollMode)}
          >
            <FormControlLabel
              value="new"
              control={<Radio />}
              label="Nuevo equipo"
            />
            <FormControlLabel
              value="existing"
              control={<Radio />}
              label="Equipo existente"
            />
          </RadioGroup>

          {mode === 'new' ? (
            <TextField
              label="Nombre del equipo"
              value={newTeamName}
              onChange={event => setNewTeamName(event.target.value)}
              required
              fullWidth
            />
          ) : (
            <>
              <TextField
                select
                label="Equipo existente"
                value={existingTeamId}
                onChange={event =>
                  setExistingTeamId(event.target.value as GUID)
                }
                fullWidth
                helperText={
                  availableTeams.length === 0
                    ? 'No hay equipos disponibles para inscribir.'
                    : undefined
                }
              >
                {availableTeams.map(team => (
                  <MenuItem key={team.id} value={team.id}>
                    {team.name}
                  </MenuItem>
                ))}
              </TextField>

              {existingTeamId && (
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={copyRoster}
                      onChange={event => setCopyRoster(event.target.checked)}
                    />
                  }
                  label="Copiar plantel de su temporada anterior"
                />
              )}

              {existingTeamId &&
                copyRoster &&
                !selectedTeam?.tournamentId && (
                  <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                    El equipo seleccionado no tiene una temporada anterior desde
                    la cual copiar el plantel.
                  </Typography>
                )}
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <FormButtons
          onCancel={handleClose}
          onConfirm={handleConfirm}
          confirmLabel="Inscribir"
          disabled={submitting}
        />
      </DialogActions>
    </Dialog>
  );
};

export default EnrollTeamDialog;
