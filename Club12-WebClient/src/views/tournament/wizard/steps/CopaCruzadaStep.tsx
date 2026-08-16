import {
  Box,
  Chip,
  Divider,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { CrossCupConfig, ROUND_ROBIN_LEGS_OPTIONS } from '../types';
import CupsEditor from './CupsEditor';

interface CopaCruzadaStepProps {
  teams: ITeamResponse[];
  value: CrossCupConfig;
  onChange: (value: CrossCupConfig) => void;
}

/**
 * The optional cup that draws teams from every zone at once. Structurally
 * identical to a zone (name, teams, group stage, playoff cups) — its only
 * special behavior is that it is marked as a cross-division cup, so a
 * team can be here AND in its regular zone at the same time.
 */
export default function CopaCruzadaStep({ teams, value, onChange }: CopaCruzadaStepProps) {
  const toggleTeam = (teamId: GUID) => {
    onChange({
      ...value,
      teamIds: value.teamIds.includes(teamId)
        ? value.teamIds.filter(id => id !== teamId)
        : [...value.teamIds, teamId],
    });
  };

  return (
    <Stack spacing={2}>
      <FormControlLabel
        control={
          <Switch
            checked={value.enabled}
            onChange={e => onChange({ ...value, enabled: e.target.checked })}
          />
        }
        label="Incluir una copa cruzada entre zonas"
      />

      {!value.enabled && (
        <Typography variant="body2" color="text.secondary">
          Opcional — podés dejarla desactivada y el torneo se arma solo con las zonas del paso anterior.
        </Typography>
      )}

      {value.enabled && (
        <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
          <TextField
            label="Nombre de la copa (libre)"
            size="small"
            value={value.name}
            onChange={e => onChange({ ...value, name: e.target.value })}
            fullWidth
            sx={{ mb: 2 }}
          />

          <FormControlLabel
            control={
              <Switch
                checked={value.includeAllTeams}
                onChange={e => onChange({ ...value, includeAllTeams: e.target.checked })}
              />
            }
            label="Incluir todos los equipos inscriptos"
          />

          {!value.includeAllTeams && (
            <>
              <Typography variant="caption" color="text.secondary" display="block" mt={1}>
                Equipos de la copa ({value.teamIds.length})
              </Typography>
              <Stack direction="row" flexWrap="wrap" gap={1} mt={0.5} mb={2}>
                {teams.map(team => (
                  <Chip
                    key={team.id}
                    label={team.name}
                    color={value.teamIds.includes(team.id) ? 'primary' : 'default'}
                    variant={value.teamIds.includes(team.id) ? 'filled' : 'outlined'}
                    onClick={() => toggleTeam(team.id)}
                  />
                ))}
              </Stack>
            </>
          )}

          <Stack direction="row" spacing={2} alignItems="center" my={2}>
            <FormControlLabel
              control={
                <Switch
                  checked={value.hasGroupStage}
                  onChange={e => onChange({ ...value, hasGroupStage: e.target.checked })}
                />
              }
              label="Fase de grupos"
            />
            {value.hasGroupStage && (
              <TextField
                select
                size="small"
                label="Veces que se enfrenta cada par"
                value={value.roundRobinLegs}
                onChange={e => onChange({ ...value, roundRobinLegs: Number(e.target.value) })}
                sx={{ minWidth: 220 }}
              >
                {ROUND_ROBIN_LEGS_OPTIONS.map(option => (
                  <MenuItem key={option} value={option}>
                    {option === 1 ? 'Una vez (simple)' : `${option} veces`}
                  </MenuItem>
                ))}
              </TextField>
            )}
          </Stack>

          <Divider sx={{ mb: 2 }} />

          <Typography variant="subtitle2" mb={1}>
            Playoffs de la copa cruzada
          </Typography>
          <CupsEditor cups={value.cups} onChange={cups => onChange({ ...value, cups })} />
        </Box>
      )}
    </Stack>
  );
}
