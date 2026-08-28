import {
  Box,
  Divider,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  CupConfig,
  PlayoffMappingConfig,
  ROUND_ROBIN_LEGS_OPTIONS,
  ZoneConfig,
  createEmptyZone,
} from '../types';
import CupsEditor from './CupsEditor';
import PlayoffRangesEditor from './PlayoffRangesEditor';

interface DivisionesStepProps {
  zones: ZoneConfig[];
  onChange: (zones: ZoneConfig[]) => void;
}

export default function DivisionesStep({ zones, onChange }: DivisionesStepProps) {
  const updateZone = (zoneId: string, updates: Partial<ZoneConfig>) => {
    onChange(zones.map(zone => (zone.id === zoneId ? { ...zone, ...updates } : zone)));
  };

  const addZone = () => onChange([...zones, createEmptyZone()]);
  const removeZone = (zoneId: string) => onChange(zones.filter(zone => zone.id !== zoneId));

  return (
    <Stack spacing={3}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        Cada zona tiene un nombre libre, una fase de grupos opcional y tantas copas paralelas como
        quieras (cada una con su propio nombre y formato por ronda). Los equipos se inscriben más
        adelante y se asignan a cada zona cuando cierra la inscripción.
      </Typography>

      {zones.map(zone => (
        <Box key={zone.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{
              alignItems: "center",
              mb: 1.5
            }}>
            <TextField
              label="Nombre de la zona (libre)"
              size="small"
              value={zone.name}
              onChange={e => updateZone(zone.id, { name: e.target.value })}
              fullWidth
            />
            <IconButton aria-label="Eliminar zona" color="error" onClick={() => removeZone(zone.id)}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Stack
            direction="row"
            spacing={2}
            sx={{
              alignItems: "center",
              mb: 2
            }}>
            <FormControlLabel
              control={
                <Switch
                  checked={zone.hasGroupStage}
                  onChange={e => updateZone(zone.id, { hasGroupStage: e.target.checked })}
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
                onChange={e => updateZone(zone.id, { roundRobinLegs: Number(e.target.value) })}
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
              value={zone.pointsForWin}
              onChange={e => updateZone(zone.id, { pointsForWin: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
            />
            <TextField
              type="number"
              size="small"
              label="Puntos por derrota"
              value={zone.pointsForLoss}
              onChange={e => updateZone(zone.id, { pointsForLoss: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 0 } }}
              sx={{ width: 180 }}
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
            onChange={(cups: CupConfig[]) => updateZone(zone.id, { cups })}
          />

          <Typography variant="subtitle2" sx={{ mb: 1, mt: 2 }}>
            Clasificación a playoffs por rango
          </Typography>
          <PlayoffRangesEditor
            mappings={zone.playoffMappings}
            cups={zone.cups}
            teamCount={0}
            onChange={(playoffMappings: PlayoffMappingConfig[]) =>
              updateZone(zone.id, { playoffMappings })
            }
          />
        </Box>
      ))}

      <IconButton
        aria-label="Agregar zona"
        onClick={addZone}
        sx={{ alignSelf: 'flex-start', border: 1, borderColor: 'divider', borderRadius: 1, px: 2 }}
      >
        <AddIcon fontSize="small" sx={{ mr: 1 }} />
        <Typography variant="button">Nueva zona</Typography>
      </IconButton>
    </Stack>
  );
}
