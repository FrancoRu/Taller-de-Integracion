import { useEffect, useRef } from 'react';
import { Grid, MenuItem, TextField } from '@mui/material';
import {
  TOURNAMENT_CATEGORY_LABELS,
  TournamentCategory,
} from '@/modules/core/enum/tournament/tournamentCategory';
import { useSeason } from '@/modules/season/hook/season.hook';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { TournamentStepState } from '../types';

interface TorneoStepProps {
  value: TournamentStepState;
  onChange: (value: TournamentStepState) => void;
  /**
   * True when the wizard was launched pre-scoped to a season (from the admin
   * season hub). The season is preselected and its helper text explains where
   * it came from, but it stays editable so the admin can still change it.
   */
  seasonPreset?: boolean;
}

export default function TorneoStep({
  value,
  onChange,
  seasonPreset = false,
}: TorneoStepProps) {
  const { seasons, getSeasonsByFiltered } = useSeason();
  const getSeasonsRef = useRef(getSeasonsByFiltered);

  useEffect(() => {
    getSeasonsRef.current = getSeasonsByFiltered;
  }, [getSeasonsByFiltered]);

  useEffect(() => {
    void getSeasonsRef.current({
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
  }, []);

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

      <Grid size={12}>
        <TextField
          select
          label="Categoría"
          value={value.category}
          onChange={e =>
            onChange({
              ...value,
              category: e.target.value as TournamentCategory,
            })
          }
          required
          fullWidth
        >
          {Object.values(TournamentCategory).map(category => (
            <MenuItem key={category} value={category}>
              {TOURNAMENT_CATEGORY_LABELS[category]}
            </MenuItem>
          ))}
        </TextField>
      </Grid>

      <Grid size={12}>
        <TextField
          select
          label="Temporada"
          value={value.seasonId ?? ''}
          onChange={e => onChange({ ...value, seasonId: e.target.value })}
          required
          fullWidth
          // A tournament always belongs to a season. When the wizard is launched
          // from a season hub the season is fixed and cannot be changed here.
          disabled={seasonPreset}
        >
          {(seasons ?? []).map(season => (
            <MenuItem key={season.id} value={season.id}>
              {season.name}
            </MenuItem>
          ))}
        </TextField>
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
