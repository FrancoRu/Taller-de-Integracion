import {
  Box,
  Divider,
  FormControlLabel,
  IconButton,
  InputAdornment,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { DeleteIcon } from '@/views/core/MUI/icons/icons';
import FieldInfoTooltip from '@/views/core/components/FieldInfoTooltip';
import { CupConfig, ROUND_ROBIN_LEGS_OPTIONS, ZoneConfig } from '../types';
import CupsEditor from './CupsEditor';

interface ZoneEditorProps {
  zone: ZoneConfig;
  onChange: (updates: Partial<ZoneConfig>) => void;
  /** Omit to render without a delete affordance (a single standalone zone). */
  onRemove?: () => void;
}

/**
 * One zone's full configuration: name, optional group stage, per-division
 * scoring (HU-79) and its parallel playoff cups (HU-112). Extracted from
 * {@link DivisionesStep} so the same editor also backs a standalone
 * "Nueva división" page — a division added to an existing tournament gets
 * the exact same structure (group stage + cups + playoff mappings) a
 * wizard-created zone gets, instead of a bare shell with no stages.
 */
export default function ZoneEditor({ zone, onChange, onRemove }: ZoneEditorProps) {
  return (
    <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
      <Stack
        direction="row"
        spacing={1}
        sx={{
          alignItems: 'center',
          mb: 1.5,
        }}>
        <TextField
          label="Nombre de la zona (libre)"
          size="small"
          value={zone.name}
          onChange={e => onChange({ name: e.target.value })}
          fullWidth
        />
        {onRemove && (
          <IconButton aria-label="Eliminar zona" color="error" onClick={onRemove}>
            <DeleteIcon fontSize="small" />
          </IconButton>
        )}
      </Stack>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{
          alignItems: { xs: 'flex-start', sm: 'center' },
          mb: 2,
        }}>
        <FormControlLabel
          control={
            <Switch
              checked={zone.hasGroupStage}
              onChange={e => onChange({ hasGroupStage: e.target.checked })}
            />
          }
          label="Fase de grupos"
        />
        {zone.hasGroupStage && (
          <TextField
            select
            size="small"
            label="Veces que se enfrenta cada par"
            value={zone.roundRobinLegs}
            onChange={e => onChange({ roundRobinLegs: Number(e.target.value) })}
            sx={{ minWidth: { xs: '100%', sm: 220 } }}
          >
            {ROUND_ROBIN_LEGS_OPTIONS.map(option => (
              <MenuItem key={option} value={option}>
                {option === 1 ? 'Una vez (simple)' : `${option} veces`}
              </MenuItem>
            ))}
          </TextField>
        )}
        {zone.hasGroupStage && (
          <TextField
            type="number"
            size="small"
            label="Cantidad de sub-grupos"
            value={zone.subGroupCount}
            onChange={e => onChange({ subGroupCount: Number(e.target.value) })}
            slotProps={{
              htmlInput: { min: 1 },
              input: {
                endAdornment: (
                  <InputAdornment position="end">
                    <FieldInfoTooltip title="El sistema reparte los equipos inscriptos lo más parejo posible entre esta cantidad de sub-grupos (nunca una diferencia de 2 o más equipos entre el más chico y el más grande, mínimo 4 equipos por sub-grupo). Dejalo en 1 para una única fase de grupos, como hoy." />
                  </InputAdornment>
                ),
              },
            }}
            sx={{ width: { xs: '100%', sm: 200 } }}
          />
        )}
      </Stack>

      {/* Per-division scoring (HU-79): defaults 2/1, no draw points. */}
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{
          alignItems: { xs: 'stretch', sm: 'center' },
          mb: 2,
        }}>
        <TextField
          type="number"
          size="small"
          label="Puntos por victoria"
          value={zone.pointsForWin}
          onChange={e => onChange({ pointsForWin: Number(e.target.value) })}
          slotProps={{ htmlInput: { min: 0 } }}
          sx={{ width: { xs: '100%', sm: 180 } }}
        />
        <TextField
          type="number"
          size="small"
          label="Puntos por derrota"
          value={zone.pointsForLoss}
          onChange={e => onChange({ pointsForLoss: Number(e.target.value) })}
          slotProps={{ htmlInput: { min: 0 } }}
          sx={{ width: { xs: '100%', sm: 180 } }}
        />
      </Stack>

      <Divider sx={{ mb: 2 }} />

      <Typography variant="subtitle2" sx={{
        mb: 1
      }}>
        Playoffs de {zone.name || 'esta zona'}
      </Typography>
      <CupsEditor
        cups={zone.cups}
        onChange={(cups: CupConfig[]) => onChange({ cups })}
      />
    </Box>
  );
}
