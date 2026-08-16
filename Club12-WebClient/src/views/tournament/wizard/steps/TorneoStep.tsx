import { Grid, TextField } from '@mui/material';
import { TournamentStepState } from '../types';

interface TorneoStepProps {
  value: TournamentStepState;
  onChange: (value: TournamentStepState) => void;
}

export default function TorneoStep({ value, onChange }: TorneoStepProps) {
  return (
    <Grid container spacing={2}>
      <Grid item xs={12}>
        <TextField
          label="Nombre"
          value={value.name}
          onChange={e => onChange({ ...value, name: e.target.value })}
          required
          fullWidth
        />
      </Grid>

      <Grid item xs={12}>
        <TextField
          label="Descripción"
          value={value.description}
          onChange={e => onChange({ ...value, description: e.target.value })}
          multiline
          minRows={2}
          fullWidth
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <TextField
          label="Inicio"
          type="date"
          value={value.startDate}
          onChange={e => onChange({ ...value, startDate: e.target.value })}
          InputLabelProps={{ shrink: true }}
          required
          fullWidth
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <TextField
          label="Límite de inscripción"
          type="date"
          value={value.teamRegistrationDeadline}
          onChange={e => onChange({ ...value, teamRegistrationDeadline: e.target.value })}
          InputLabelProps={{ shrink: true }}
          required
          fullWidth
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <TextField
          label="Mín. equipos"
          type="number"
          value={value.minTeams}
          onChange={e => onChange({ ...value, minTeams: Number(e.target.value) })}
          fullWidth
        />
      </Grid>

      <Grid item xs={12} md={6}>
        <TextField
          label="Máx. equipos"
          type="number"
          value={value.maxTeams}
          onChange={e => onChange({ ...value, maxTeams: Number(e.target.value) })}
          fullWidth
        />
      </Grid>
    </Grid>
  );
}
