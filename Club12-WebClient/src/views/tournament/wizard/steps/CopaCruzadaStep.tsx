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
import { CrossCupConfig, PlayoffMappingConfig, ROUND_ROBIN_LEGS_OPTIONS } from '../types';
import CupsEditor from './CupsEditor';
import PlayoffRangesEditor from './PlayoffRangesEditor';

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
        <Typography variant="body2" sx={{
          color: "text.secondary"
        }}>
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
              <Typography
                variant="caption"
                sx={{
                  color: "text.secondary",
                  display: "block",
                  mt: 1
                }}>
                Equipos de la copa ({value.teamIds.length})
              </Typography>
              <Stack
                direction="row"
                sx={{
                  flexWrap: "wrap",
                  gap: 1,
                  mt: 0.5,
                  mb: 2
                }}>
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

          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: "center",
              my: 2
            }}>
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

          {/* Per-division scoring (HU-79): defaults 2/1, no draw points. */}
          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: 'center',
              mb: 2,
            }}>
            <TextField
              type="number"
              size="small"
              label="Puntos por victoria"
              value={value.pointsForWin}
              onChange={e => onChange({ ...value, pointsForWin: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
            />
            <TextField
              type="number"
              size="small"
              label="Puntos por derrota"
              value={value.pointsForLoss}
              onChange={e => onChange({ ...value, pointsForLoss: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
            />
          </Stack>

          <Divider sx={{ mb: 2 }} />

          <Typography variant="subtitle2" sx={{
            mb: 1
          }}>
            Playoffs de la copa cruzada
          </Typography>
          <CupsEditor cups={value.cups} onChange={cups => onChange({ ...value, cups })} />

          <Typography variant="subtitle2" sx={{ mb: 1, mt: 2 }}>
            Clasificación a playoffs por rango
          </Typography>
          <PlayoffRangesEditor
            mappings={value.playoffMappings}
            cups={value.cups}
            teamCount={value.includeAllTeams ? teams.length : value.teamIds.length}
            onChange={(playoffMappings: PlayoffMappingConfig[]) =>
              onChange({ ...value, playoffMappings })
            }
          />
        </Box>
      )}
    </Stack>
  );
}
