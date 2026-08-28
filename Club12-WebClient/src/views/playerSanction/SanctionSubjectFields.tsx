import { Grid, MenuItem, TextField } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { SanctionSubjectType } from '@/modules/playerSanction/type/playerSanction.d';
import { SANCTION_SUBJECT_TYPE_LABEL } from '@/modules/playerSanction/utils/sanctionDisplay';

interface SubjectTeamOption {
  id: GUID;
  name: string;
}

interface SubjectPlayerOption {
  id: GUID;
  fullName: string;
}

interface SanctionSubjectFieldsProps {
  subjectType: SanctionSubjectType;
  teamId: GUID | '';
  playerId: GUID | '';
  staffName: string;
  teamOptions: SubjectTeamOption[];
  playerOptions: SubjectPlayerOption[];
  /** Disables the identity inputs until the match (and thus its teams) is chosen. */
  disabled?: boolean;
  onSubjectTypeChange: (value: SanctionSubjectType) => void;
  onTeamChange: (value: GUID | '') => void;
  onPlayerChange: (value: GUID | '') => void;
  onStaffNameChange: (value: string) => void;
}

const SUBJECT_TYPES: SanctionSubjectType[] = ['Player', 'Team', 'Staff'];

/**
 * The subject-type selector plus the matching identity input for a sanction
 * (HU-77): a player picker (Jugador), a team picker (Equipo), or a free-text
 * name (Staff). Presentational and fully controlled so it can be unit-tested in
 * isolation — the create form owns the state and payload assembly.
 */
export default function SanctionSubjectFields({
  subjectType,
  teamId,
  playerId,
  staffName,
  teamOptions,
  playerOptions,
  disabled = false,
  onSubjectTypeChange,
  onTeamChange,
  onPlayerChange,
  onStaffNameChange,
}: SanctionSubjectFieldsProps) {
  const showTeam = subjectType === 'Player' || subjectType === 'Team';
  const showPlayer = subjectType === 'Player';
  const showStaff = subjectType === 'Staff';

  return (
    <>
      <Grid size={{ xs: 12, md: 6 }}>
        <TextField
          select
          required
          label="Tipo de sanción"
          value={subjectType}
          onChange={e => onSubjectTypeChange(e.target.value as SanctionSubjectType)}
          fullWidth
        >
          {SUBJECT_TYPES.map(type => (
            <MenuItem key={type} value={type}>
              {SANCTION_SUBJECT_TYPE_LABEL[type]}
            </MenuItem>
          ))}
        </TextField>
      </Grid>

      {showTeam && (
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            select
            required
            label="Equipo"
            value={teamId}
            onChange={e => onTeamChange(e.target.value as GUID | '')}
            disabled={disabled}
            fullWidth
          >
            <MenuItem value="" disabled>
              Seleccionar equipo
            </MenuItem>
            {teamOptions.map(teamOption => (
              <MenuItem key={teamOption.id} value={teamOption.id}>
                {teamOption.name}
              </MenuItem>
            ))}
          </TextField>
        </Grid>
      )}

      {showPlayer && (
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            select
            required
            label="Jugador"
            value={playerId}
            onChange={e => onPlayerChange(e.target.value as GUID | '')}
            disabled={disabled || !teamId}
            fullWidth
          >
            <MenuItem value="" disabled>
              Seleccionar jugador
            </MenuItem>
            {playerOptions.map(playerOption => (
              <MenuItem key={playerOption.id} value={playerOption.id}>
                {playerOption.fullName}
              </MenuItem>
            ))}
          </TextField>
        </Grid>
      )}

      {showStaff && (
        <Grid size={{ xs: 12, md: 6 }}>
          <TextField
            label="Nombre del staff"
            value={staffName}
            onChange={e => onStaffNameChange(e.target.value)}
            required
            fullWidth
          />
        </Grid>
      )}
    </>
  );
}
