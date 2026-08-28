import { useCallback } from 'react';
import {
  MenuItem,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
} from '@mui/material';
import { ScorerScope } from '@/modules/scorer/type/scorer.d';

export interface ScorerScopeToggleProps {
  /** The currently selected HU-85 ranking scope. */
  scope: ScorerScope;
  onScopeChange: (scope: ScorerScope) => void;
  /** Selected season year, only relevant while `scope === 'season'`. */
  season: number | '';
  onSeasonChange: (season: number | '') => void;
  /** Season years the ranking can be scoped to, most recent first. */
  seasonOptions: number[];
}

const SCOPE_LABELS: Record<ScorerScope, string> = {
  tournament: 'Por torneo',
  season: 'Por temporada',
  allTime: 'Histórico (todos los tiempos)',
};

const SCOPE_ORDER: ScorerScope[] = ['tournament', 'season', 'allTime'];

/**
 * HU-85 scope selector for the goleadores ranking: per tournament, per season
 * (with a year picker) or all-time. Purely presentational — the parent owns
 * the scope/season state and turns it into the ranking query params.
 */
const ScorerScopeToggle: React.FC<ScorerScopeToggleProps> = ({
  scope,
  onScopeChange,
  season,
  onSeasonChange,
  seasonOptions,
}) => {
  const handleScopeChange = useCallback(
    (_: React.MouseEvent<HTMLElement>, value: ScorerScope | null) => {
      if (value) {
        onScopeChange(value);
      }
    },
    [onScopeChange]
  );

  const handleSeasonChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      const { value } = event.target;
      onSeasonChange(value === '' ? '' : Number(value));
    },
    [onSeasonChange]
  );

  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={2}
      sx={{ alignItems: { sm: 'center' }, flexWrap: 'wrap' }}
    >
      <ToggleButtonGroup
        exclusive
        size="small"
        color="primary"
        value={scope}
        onChange={handleScopeChange}
        aria-label="Alcance del ranking"
      >
        {SCOPE_ORDER.map(value => (
          <ToggleButton key={value} value={value}>
            {SCOPE_LABELS[value]}
          </ToggleButton>
        ))}
      </ToggleButtonGroup>

      {scope === 'season' && (
        <TextField
          select
          label="Temporada"
          size="small"
          value={season === '' ? '' : String(season)}
          onChange={handleSeasonChange}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Todas</MenuItem>
          {seasonOptions.map(year => (
            <MenuItem key={year} value={String(year)}>
              {year}
            </MenuItem>
          ))}
        </TextField>
      )}
    </Stack>
  );
};

export default ScorerScopeToggle;
