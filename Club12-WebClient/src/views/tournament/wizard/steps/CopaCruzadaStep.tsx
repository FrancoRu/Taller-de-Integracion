import {
  Box,
  Divider,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { CrossCupConfig, PlayoffMappingConfig, ROUND_ROBIN_LEGS_OPTIONS } from '../types';
import CupsEditor from './CupsEditor';
import PlayoffRangesEditor from './PlayoffRangesEditor';

interface CopaCruzadaStepProps {
  value: CrossCupConfig;
  onChange: (value: CrossCupConfig) => void;
}

/**
 * The optional cup that spans every zone at once. Structurally identical to
 * a zone (name, group stage, playoff cups) — its only special behavior is
 * that it is marked as a cross-division cup, so a team can eventually be
 * here AND in its regular zone at the same time. HU-106: no teams are
 * selected here; the wizard defines structure only.
 */
export default function CopaCruzadaStep({ value, onChange }: CopaCruzadaStepProps) {
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

          {/* HU-110: the cross cup is split into N groups; the top
              `qualifiersPerGroup` of each group are pooled into the bracket. */}
          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: "center",
              flexWrap: 'wrap',
              gap: 2,
              my: 2
            }}>
            <TextField
              type="number"
              size="small"
              label="Cantidad de grupos"
              value={value.groupCount}
              onChange={e => onChange({ ...value, groupCount: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 1 } }}
              sx={{ width: 180 }}
            />
            <TextField
              type="number"
              size="small"
              label="Clasifican por grupo"
              value={value.qualifiersPerGroup}
              onChange={e => onChange({ ...value, qualifiersPerGroup: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 1 } }}
              sx={{ width: 180 }}
            />
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
            teamCount={0}
            onChange={(playoffMappings: PlayoffMappingConfig[]) =>
              onChange({ ...value, playoffMappings })
            }
          />
        </Box>
      )}
    </Stack>
  );
}
