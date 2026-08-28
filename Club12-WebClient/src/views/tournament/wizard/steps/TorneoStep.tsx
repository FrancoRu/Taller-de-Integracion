import { Grid, TextField } from '@mui/material';
import { TournamentStepState } from '../types';

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
    </Grid>
  );
}
