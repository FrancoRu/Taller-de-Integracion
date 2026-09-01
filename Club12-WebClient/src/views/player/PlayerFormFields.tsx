import { useMemo } from 'react';
import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import {
  isAtLeastMinimumPlayerAge,
  isValidDocumentNumber,
  isValidPhone,
  VALIDATION_MESSAGES,
} from '@/modules/core/utils/validators';
import FieldInfoTooltip from '@/views/core/components/FieldInfoTooltip';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import type { PlayerFormField, PlayerFormState } from '@/views/player/players.types';

/** Latest birth date an `<input type="date">` should accept: today minus the
 * minimum player age, so the picker itself steers users away from an
 * underage date instead of only rejecting it on submit. */
const getMaxBirthDate = () => {
  const date = new Date();
  date.setFullYear(date.getFullYear() - 15);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export interface PlayerDorsalFieldConfig {
  value: string;
  disabled: boolean;
  helperText: string;
  onChange: (value: string) => void;
}

export interface PlayerFormFieldsProps {
  form: PlayerFormState;
  onFieldChange: (field: PlayerFormField, value: string) => void;
  /** Whether the team select renders at all — omitted when the team is
   * already fixed by the surrounding context (e.g. creating a player
   * directly from a team's roster). */
  showTeamSelect: boolean;
  teamOptions: ITeamResponse[];
  onTeamChange: (teamId: GUID) => void;
  /** Only present when editing from within a team roster context (HU-54) —
   * a standalone player detail page has no team+tournament scope to assign
   * a dorsal against. */
  dorsalField?: PlayerDorsalFieldConfig;
}

/**
 * The identity fields shared by every place a player gets created or
 * edited — a single source of truth so a player created/edited from the
 * standalone Jugadores admin form, a team's roster, or the player's own
 * detail page all enforce the same required fields and validation.
 */
export default function PlayerFormFields({
  form,
  onFieldChange,
  showTeamSelect,
  teamOptions,
  onTeamChange,
  dorsalField,
}: PlayerFormFieldsProps) {
  const maxBirthDate = useMemo(getMaxBirthDate, []);
  const phoneError = form.phoneNumber.length > 0 && !isValidPhone(form.phoneNumber);
  const documentNumberError =
    form.documentNumber.length > 0 && !isValidDocumentNumber(form.documentNumber);
  const birthDateError =
    form.birthDate.length > 0 && !isAtLeastMinimumPlayerAge(form.birthDate);

  return (
    <Stack spacing={2} sx={{ mt: 1 }}>
      <TextField
        label="Nombre"
        value={form.firstName}
        onChange={e => onFieldChange('firstName', e.target.value)}
        required
        fullWidth
      />
      <TextField
        label="Segundo nombre"
        value={form.secondName}
        onChange={e => onFieldChange('secondName', e.target.value)}
        fullWidth
        slotProps={{
          input: {
            endAdornment: (
              <FieldInfoTooltip title="Opcional, por si el jugador tiene un segundo nombre." />
            ),
          },
        }}
      />
      <TextField
        label="Apellido"
        value={form.lastName}
        onChange={e => onFieldChange('lastName', e.target.value)}
        required
        fullWidth
      />
      <TextField
        label="Documento"
        value={form.documentNumber}
        onChange={e => onFieldChange('documentNumber', e.target.value.replace(/\D/g, ''))}
        required
        fullWidth
        error={documentNumberError}
        helperText={documentNumberError ? VALIDATION_MESSAGES.documentNumber : undefined}
      />
      <TextField
        label="Fecha de nacimiento"
        type="date"
        value={form.birthDate}
        onChange={e => onFieldChange('birthDate', e.target.value)}
        fullWidth
        error={birthDateError}
        helperText={birthDateError ? VALIDATION_MESSAGES.minimumPlayerAge : undefined}
        slotProps={{
          inputLabel: { shrink: true },
          htmlInput: { max: maxBirthDate },
        }}
      />
      <TextField
        label="Teléfono"
        value={form.phoneNumber}
        onChange={e => onFieldChange('phoneNumber', e.target.value)}
        fullWidth
        error={phoneError}
        helperText={phoneError ? VALIDATION_MESSAGES.phone : undefined}
      />
      <TextField
        label="Obra social"
        value={form.socialSecurity}
        onChange={e => onFieldChange('socialSecurity', e.target.value)}
        fullWidth
      />

      {dorsalField && (
        <TextField
          label="Dorsal"
          type="number"
          value={dorsalField.value}
          onChange={e => dorsalField.onChange(e.target.value)}
          fullWidth
          disabled={dorsalField.disabled}
          helperText={dorsalField.helperText}
          slotProps={{ htmlInput: { min: 0, max: 99, step: 1 } }}
        />
      )}

      {showTeamSelect && (
        <FormControl fullWidth required>
          <InputLabel id="player-team-select-label">Equipo</InputLabel>
          <Select
            labelId="player-team-select-label"
            label="Equipo"
            value={form.teamId}
            onChange={e => onTeamChange(e.target.value as GUID)}
          >
            {teamOptions.map(teamOption => (
              <MenuItem key={teamOption.id} value={teamOption.id}>
                {teamOption.name}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      )}
    </Stack>
  );
}
