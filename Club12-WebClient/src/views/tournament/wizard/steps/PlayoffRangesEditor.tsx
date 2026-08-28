import { Alert, Box, Button, IconButton, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  CupConfig,
  PlayoffMappingConfig,
  createEmptyPlayoffMapping,
} from '../types';
import { validatePlayoffMappings } from '../wizardLogic';

interface PlayoffRangesEditorProps {
  mappings: PlayoffMappingConfig[];
  /** The division's configured cups — their names are the selectable destinations. */
  cups: CupConfig[];
  /** How many teams the division has, used for the range upper bound. */
  teamCount: number;
  onChange: (mappings: PlayoffMappingConfig[]) => void;
}

/**
 * Per-division playoff-range editor (HU-45): rows of (from position, to
 * position, destination cup) that map final group-stage standings to each
 * cup. Destinations come only from the division's configured cups (never a
 * hardcoded "Oro"/"Plata"). Ranges must not overlap and must stay within
 * the team count — this is validated live here (for immediate feedback) and
 * again on "Continuar" (see wizardLogic.validatePlayoffMappings).
 */
export default function PlayoffRangesEditor({
  mappings,
  cups,
  teamCount,
  onChange,
}: PlayoffRangesEditorProps) {
  const cupNames = cups.map(cup => cup.name.trim()).filter(Boolean);

  const addMapping = () => onChange([...mappings, createEmptyPlayoffMapping()]);
  const removeMapping = (id: string) => onChange(mappings.filter(mapping => mapping.id !== id));

  const updateMapping = (id: string, updates: Partial<PlayoffMappingConfig>) =>
    onChange(mappings.map(mapping => (mapping.id === id ? { ...mapping, ...updates } : mapping)));

  const liveErrors = validatePlayoffMappings(mappings, teamCount, cupNames, 'esta zona');

  return (
    <Stack spacing={1.5}>
      <Typography variant="caption" sx={{ color: 'text.secondary' }}>
        Clasificación a playoffs por rango de posición. Ejemplo (10 equipos): 1–4 → Copa Oro,
        5–8 → Copa Plata, 9–10 sin playoff. Los rangos no pueden solaparse ni superar la cantidad
        de equipos de la zona.
      </Typography>

      {cupNames.length === 0 && (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Agregá al menos una copa arriba para poder mapear posiciones a un destino.
        </Typography>
      )}

      {cupNames.length > 0 &&
        mappings.map(mapping => (
          <Stack key={mapping.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <TextField
              type="number"
              size="small"
              label="Desde"
              value={mapping.fromPosition}
              onChange={e => updateMapping(mapping.id, { fromPosition: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 1, max: teamCount || undefined } }}
              sx={{ width: 100 }}
            />
            <TextField
              type="number"
              size="small"
              label="Hasta"
              value={mapping.toPosition}
              onChange={e => updateMapping(mapping.id, { toPosition: Number(e.target.value) })}
              slotProps={{ htmlInput: { min: 1, max: teamCount || undefined } }}
              sx={{ width: 100 }}
            />
            <TextField
              select
              size="small"
              label="Copa de destino"
              value={cupNames.includes(mapping.destination) ? mapping.destination : ''}
              onChange={e => updateMapping(mapping.id, { destination: e.target.value })}
              sx={{ minWidth: 200 }}
            >
              {cupNames.map(name => (
                <MenuItem key={name} value={name}>
                  {name}
                </MenuItem>
              ))}
            </TextField>
            <IconButton
              aria-label="Eliminar rango"
              size="small"
              onClick={() => removeMapping(mapping.id)}
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Stack>
        ))}

      {cupNames.length > 0 && liveErrors.length > 0 && (
        <Alert severity="warning" sx={{ py: 0 }}>
          {liveErrors[0]}
        </Alert>
      )}

      {cupNames.length > 0 && (
        <Box>
          <Button size="small" startIcon={<AddIcon />} onClick={addMapping}>
            Agregar rango
          </Button>
        </Box>
      )}
    </Stack>
  );
}
