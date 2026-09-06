import React, { useMemo, useState } from 'react';
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
import TeamFormFields from '@/views/team/TeamFormFields';
import type { TeamFormField, TeamFormState } from '@/views/team/teams.types';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse, IAddTeamRequest } from '@/modules/team/type/team.d';
import { IEnrollTeamRequest } from '@/modules/tournament/type/tournament';
import { useTeam } from '@/modules/team/hook/team.hook';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';

type EnrollMode = 'new' | 'existing';

interface EnrollTeamDialogProps {
  open: boolean;
  submitting: boolean;
  /**
   * Teams that can be enrolled, one physical `Team` row per past-or-current
   * season. The caller is expected to exclude teams already enrolled in this
   * tournament. A club is the stable identity here, not a season — see
   * {@link dedupeByClub}, which collapses these down to one pickable entry
   * per club before they ever reach the picker.
   */
  availableTeams: ITeamResponse[];
  onClose: () => void;
  onConfirm: (request: IEnrollTeamRequest) => void;
}

/**
 * Collapses one Team row per club (identified by `clubId` when linked, or by
 * normalized name otherwise — the same identity key `ClubService` itself
 * slugs a club from) down to a single representative: an organizer picks a
 * CLUB here, never a specific past season's Team row. Within a club, prefers
 * a row that still has a `tournamentId` (so "copiar plantel de su temporada
 * anterior" has a real season to read from) over one that doesn't; ties keep
 * list order.
 */
const dedupeByClub = (teams: ITeamResponse[]): ITeamResponse[] => {
  const byClub = new Map<string, ITeamResponse>();

  for (const team of teams) {
    const clubKey = team.clubId ?? `name:${team.name.trim().toLowerCase()}`;
    const current = byClub.get(clubKey);

    if (!current || (!current.tournamentId && team.tournamentId)) {
      byClub.set(clubKey, team);
    }
  }

  return [...byClub.values()];
};

const INITIAL_MODE: EnrollMode = 'new';

const INITIAL_TEAM_FORM: TeamFormState = {
  name: '',
  threeLetterCode: '',
  shirtColor: '#1E5FCC',
  shirtSecondaryColor: '',
  shirtTertiaryColor: '',
  jerseyStyle: 'solid',
  logo: null,
  logoUrl: '',
};

/**
 * Dialog to enroll a team into a tournament (HU-107). Two mutually exclusive
 * modes: create a brand-new team, or pick an existing team — with an
 * optional "copy roster from previous season" toggle.
 *
 * A brand-new team goes through the same identity fields (name, code,
 * escudo, kit) as the standalone Equipos admin form via the shared
 * {@link TeamFormFields} — enrolling a team is not an excuse to skip the
 * validations every other team must satisfy. It is created first (via
 * `addTeam`, same as the Equipos page) and then enrolled as an existing
 * team, so the enroll call itself always carries `existingTeamId`.
 */
const EnrollTeamDialog: React.FC<EnrollTeamDialogProps> = ({
  open,
  submitting,
  availableTeams,
  onClose,
  onConfirm,
}) => {
  const { addTeam } = useTeam();
  const [mode, setMode] = useState<EnrollMode>(INITIAL_MODE);
  const [teamForm, setTeamForm] = useState<TeamFormState>(INITIAL_TEAM_FORM);
  const [creatingTeam, setCreatingTeam] = useState(false);
  const [existingTeamId, setExistingTeamId] = useState<GUID | ''>('');
  const [copyRoster, setCopyRoster] = useState(false);

  const reset = () => {
    setMode(INITIAL_MODE);
    setTeamForm(INITIAL_TEAM_FORM);
    setExistingTeamId('');
    setCopyRoster(false);
  };

  const handleClose = () => {
    if (submitting || creatingTeam) {
      return;
    }
    reset();
    onClose();
  };

  const handleTeamFieldChange = (field: TeamFormField, value: string) => {
    setTeamForm(prev => ({ ...prev, [field]: value }));
  };

  const clubEntries = useMemo(() => dedupeByClub(availableTeams), [availableTeams]);
  const selectedTeam = clubEntries.find(team => team.id === existingTeamId);

  const handleConfirm = async () => {
    if (mode === 'new') {
      const name = teamForm.name.trim();
      const threeLetterCode = teamForm.threeLetterCode.trim();

      if (!name || !threeLetterCode) {
        void notifyWarning({
          title: 'Campos incompletos',
          text: 'Nombre y código son obligatorios.',
        });
        return;
      }

      if (!teamForm.logo) {
        void notifyWarning({
          title: 'Logo requerido',
          text: 'Debe seleccionar un logo para crear el equipo.',
        });
        return;
      }

      const payload: IAddTeamRequest = {
        name,
        threeLetterCode,
        shirtColor: teamForm.shirtColor.trim(),
        shirtSecondaryColor: teamForm.shirtSecondaryColor.trim() || null,
        shirtTertiaryColor: teamForm.shirtTertiaryColor.trim() || null,
        jerseyStyle: teamForm.jerseyStyle,
        logo: teamForm.logo,
      };

      setCreatingTeam(true);
      const createdTeam = await addTeam(payload);
      setCreatingTeam(false);

      if (!createdTeam) {
        return;
      }

      onConfirm({ existingTeamId: createdTeam.id });
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

  const busy = submitting || creatingTeam;

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
            <TeamFormFields
              withLogo
              form={teamForm}
              onFieldChange={handleTeamFieldChange}
              onLogoChange={file => setTeamForm(prev => ({ ...prev, logo: file }))}
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
                  clubEntries.length === 0
                    ? 'No hay equipos disponibles para inscribir.'
                    : undefined
                }
              >
                {clubEntries.map(team => (
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
          onConfirm={() => void handleConfirm()}
          confirmLabel="Inscribir"
          disabled={busy}
        />
      </DialogActions>
    </Dialog>
  );
};

export default EnrollTeamDialog;
