import { Grid, TextField } from '@mui/material';
import {
  MAX_ALLOWED_TOURNAMENT_TEAMS,
  MIN_ALLOWED_TOURNAMENT_TEAMS,
  TournamentStepState,
} from '../types';

interface TorneoStepProps {
  value: TournamentStepState;
  onChange: (value: TournamentStepState) => void;
}

export default function TorneoStep({ value, onChange }: TorneoStepProps) {
  return (
    <Grid container spacing={2}>
      <Grid size={12}>
        <TextField
          label="Nombre"
          value={value.name}
          onChange={e => onChange({ ...value, name: e.target.value })}
          required
          fullWidth
        />
      </Grid>

      <Grid size={12}>
        <TextField
          label="Descripción"
          value={value.description}
          onChange={e => onChange({ ...value, description: e.target.value })}
          multiline
          minRows={2}
          fullWidth
        />
      </Grid>

      <Grid
        size={{
          xs: 12,
          md: 6
        }}>
        <TextField
          label="Inicio"
          type="date"
          value={value.startDate}
          onChange={e => onChange({ ...value, startDate: e.target.value })}
          required
          fullWidth
          slotProps={{
            inputLabel: { shrink: true }
          }}
        />
      </Grid>

      <Grid
        size={{
          xs: 12,
          md: 6
        }}>
        <TextField
          label="Límite de inscripción"
          type="date"
          value={value.teamRegistrationDeadline}
          onChange={e => onChange({ ...value, teamRegistrationDeadline: e.target.value })}
          required
          fullWidth
          slotProps={{
            inputLabel: { shrink: true }
          }}
        />
      </Grid>

      <Grid
        size={{
          xs: 12,
          md: 6
        }}>
        <TextField
          label="Mín. equipos"
          type="number"
          value={value.minTeams}
          onChange={e => onChange({ ...value, minTeams: Number(e.target.value) })}
          helperText={`Entre ${MIN_ALLOWED_TOURNAMENT_TEAMS} y ${MAX_ALLOWED_TOURNAMENT_TEAMS}`}
          slotProps={{
            htmlInput: { min: MIN_ALLOWED_TOURNAMENT_TEAMS, max: MAX_ALLOWED_TOURNAMENT_TEAMS },
          }}
          fullWidth
        />
      </Grid>

      <Grid
        size={{
          xs: 12,
          md: 6
        }}>
        <TextField
          label="Máx. equipos"
          type="number"
          value={value.maxTeams}
          onChange={e => onChange({ ...value, maxTeams: Number(e.target.value) })}
          helperText={`Entre ${MIN_ALLOWED_TOURNAMENT_TEAMS} y ${MAX_ALLOWED_TOURNAMENT_TEAMS}`}
          slotProps={{
            htmlInput: { min: MIN_ALLOWED_TOURNAMENT_TEAMS, max: MAX_ALLOWED_TOURNAMENT_TEAMS },
          }}
          fullWidth
        />
      </Grid>
    </Grid>
  );
}
