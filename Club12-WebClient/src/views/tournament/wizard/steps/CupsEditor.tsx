import { Box, Button, IconButton, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import {
  BEST_OF_OPTIONS,
  CupConfig,
  MAX_CUP_QUALIFIERS,
  MIN_CUP_QUALIFIERS,
  createEmptyCup,
  qualifiersToStageTypes,
} from '../types';
import { STAGE_TYPE_LABELS } from '../types';

interface CupsEditorProps {
  cups: CupConfig[];
  onChange: (cups: CupConfig[]) => void;
  /**
   * When true, the per-cup "cuántos clasifican" field is hidden because the
   * qualifier count is derived elsewhere (the cross cup pools the top teams
   * of every group, HU-110/112). The bracket rounds are still derived from
   * that count at submit time.
   */
  hideQualifiers?: boolean;
}

/**
 * HU-112: lets the admin build zero or more parallel playoff cups for a zone
 * (or the cross-division cup). Each cup is a free-text name plus HOW MANY
 * teams qualify to it and the series format — the bracket rounds are DERIVED
 * from the qualifier count (shown live), so a cup can never be configured with
 * fewer rounds than its qualifiers need. "Copa de Oro"/"Copa de Plata" are
 * never suggested — the admin types whatever name they want.
 */
export default function CupsEditor({ cups, onChange, hideQualifiers = false }: CupsEditorProps) {
  const updateCup = (cupId: string, updates: Partial<CupConfig>) => {
    onChange(cups.map(cup => (cup.id === cupId ? { ...cup, ...updates } : cup)));
  };

  const addCup = () => onChange([...cups, createEmptyCup()]);
  const removeCup = (cupId: string) => onChange(cups.filter(cup => cup.id !== cupId));

  const roundsPreview = (qualifiers: number): string =>
    qualifiersToStageTypes(qualifiers)
      .map(stageType => STAGE_TYPE_LABELS[stageType])
      .join(' → ');

  return (
    <Stack spacing={2}>
      {cups.map(cup => (
        <Box key={cup.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
            <TextField
              label="Nombre de la copa (libre)"
              size="small"
              value={cup.name}
              onChange={e => updateCup(cup.id, { name: e.target.value })}
              fullWidth
            />
            <IconButton aria-label="Eliminar copa" color="error" onClick={() => removeCup(cup.id)}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Stack direction="row" spacing={2} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            {!hideQualifiers && (
              <TextField
                type="number"
                size="small"
                label="Cuántos clasifican"
                value={cup.qualifiers}
                onChange={e => updateCup(cup.id, { qualifiers: Number(e.target.value) })}
                slotProps={{ htmlInput: { min: MIN_CUP_QUALIFIERS, max: MAX_CUP_QUALIFIERS } }}
                sx={{ width: 180 }}
              />
            )}

            <TextField
              select
              size="small"
              label="Serie (formato)"
              value={cup.bestOf}
              onChange={e => updateCup(cup.id, { bestOf: Number(e.target.value) })}
              sx={{ minWidth: 180 }}
            >
              {BEST_OF_OPTIONS.map(option => (
                <MenuItem key={option} value={option}>
                  {option === 1 ? 'Partido único' : `Al mejor de ${option}`}
                </MenuItem>
              ))}
            </TextField>
          </Stack>

          {!hideQualifiers && (
            <Typography variant="caption" sx={{ color: 'text.secondary', mt: 1, display: 'block' }}>
              Fases: {roundsPreview(cup.qualifiers)}
            </Typography>
          )}
        </Box>
      ))}

      {cups.length === 0 && (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Sin copas configuradas. Podés agregar una o más — cada una con su nombre, cuántos clasifican
          y el formato de serie. Las fases del cuadro se arman solas según los clasificados.
        </Typography>
      )}

      <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={addCup} sx={{ alignSelf: 'flex-start' }}>
        Agregar copa
      </Button>
    </Stack>
  );
}
