import {
  Box,
  Button,
  Checkbox,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import { StageType } from '@/modules/stage/type/stage';
import {
  BEST_OF_OPTIONS,
  CupConfig,
  MAX_CUP_QUALIFIERS,
  MIN_CUP_QUALIFIERS,
  STAGE_TYPE_LABELS,
  createEmptyCup,
  cupPositionRange,
  getStageBestOf,
  qualifiersToStageTypes,
} from '../types';

interface CupsEditorProps {
  cups: CupConfig[];
  onChange: (cups: CupConfig[]) => void;
  /**
   * When true the per-cup "cuántos clasifican" field and the positions hint are
   * hidden because the qualifier count is derived elsewhere (the cross cup
   * pools the top teams of every group, HU-110/112). Pass `derivedQualifiers`
   * so the phase/format rows still reflect the real bracket size.
   */
  hideQualifiers?: boolean;
  /** The derived qualifier count to use when `hideQualifiers` is true (cross cup). */
  derivedQualifiers?: number;
}

const bestOfLabel = (bestOf: number): string =>
  bestOf === 1 ? 'Partido único' : `Al mejor de ${bestOf}`;

const positionsHint = (from: number, to: number): string =>
  from === to ? `Clasifica el puesto #${from}` : `Clasifican los puestos #${from} al #${to}`;

/**
 * HU-112: builds zero or more parallel playoff cups for a zone (or the cross
 * cup). Each cup is a free-text name plus HOW MANY teams qualify — the bracket
 * phases are DERIVED from that count, and EACH derived phase gets its own
 * series format (so semis can be best-of-3 and the final best-of-5). A hint
 * spells out which standings positions qualify to each cup (#1–#4, #5–#8, …),
 * all derived from the cups' order.
 */
export default function CupsEditor({
  cups,
  onChange,
  hideQualifiers = false,
  derivedQualifiers,
}: CupsEditorProps) {
  const updateCup = (cupId: string, updates: Partial<CupConfig>) => {
    onChange(cups.map(cup => (cup.id === cupId ? { ...cup, ...updates } : cup)));
  };

  const setStageBestOf = (cup: CupConfig, stageType: StageType, bestOf: number) =>
    updateCup(cup.id, { bestOfByStage: { ...cup.bestOfByStage, [stageType]: bestOf } });

  const addCup = () => onChange([...cups, createEmptyCup()]);
  const removeCup = (cupId: string) => onChange(cups.filter(cup => cup.id !== cupId));

  return (
    <Stack spacing={2}>
      {cups.map((cup, index) => {
        const effectiveQualifiers = hideQualifiers ? derivedQualifiers ?? cup.qualifiers : cup.qualifiers;
        // A third-place decider needs semifinal losers to seed it from — a
        // 2-qualifier cup (Final only) has none, so the toggle is meaningless
        // (and hidden) below that.
        const hasSemiFinal = effectiveQualifiers > 2;
        const phases = qualifiersToStageTypes(effectiveQualifiers, cup.hasThirdPlace);
        const range = cupPositionRange(cups, index);

        return (
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

            {!hideQualifiers && (
              <Stack direction="row" spacing={2} sx={{ alignItems: 'center', mb: 0.5 }}>
                <TextField
                  type="number"
                  size="small"
                  label="Cuántos clasifican"
                  value={cup.qualifiers}
                  onChange={e => updateCup(cup.id, { qualifiers: Number(e.target.value) })}
                  slotProps={{ htmlInput: { min: MIN_CUP_QUALIFIERS, max: MAX_CUP_QUALIFIERS } }}
                  sx={{ width: 180 }}
                />
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  {cup.name.trim() ? positionsHint(range.from, range.to) : 'Poné un nombre para asignar los puestos'}
                </Typography>
              </Stack>
            )}

            {hasSemiFinal && (
              <FormControlLabel
                control={
                  <Checkbox
                    size="small"
                    checked={cup.hasThirdPlace}
                    onChange={e => updateCup(cup.id, { hasThirdPlace: e.target.checked })}
                  />
                }
                label={
                  <Typography variant="body2">
                    Jugar partido por el tercer puesto (perdedores de semifinal)
                  </Typography>
                }
              />
            )}

            <Typography variant="caption" sx={{ color: 'text.secondary', mt: 1, mb: 0.5, display: 'block' }}>
              Formato por fase:
            </Typography>
            <Stack spacing={1}>
              {phases.map(stageType => (
                <Stack key={stageType} direction="row" spacing={2} sx={{ alignItems: 'center' }}>
                  <Typography variant="body2" sx={{ minWidth: 140 }}>
                    {STAGE_TYPE_LABELS[stageType]}
                  </Typography>
                  <TextField
                    select
                    size="small"
                    label="Formato"
                    value={getStageBestOf(cup, stageType)}
                    onChange={e => setStageBestOf(cup, stageType, Number(e.target.value))}
                    sx={{ minWidth: 170 }}
                  >
                    {BEST_OF_OPTIONS.map(option => (
                      <MenuItem key={option} value={option}>
                        {bestOfLabel(option)}
                      </MenuItem>
                    ))}
                  </TextField>
                </Stack>
              ))}
            </Stack>
          </Box>
        );
      })}

      {cups.length === 0 && (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Sin copas configuradas. Podés agregar una o más — cada una con su nombre, cuántos clasifican
          y el formato de cada fase. Las fases del cuadro se arman solas según los clasificados.
        </Typography>
      )}

      <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={addCup} sx={{ alignSelf: 'flex-start' }}>
        Agregar copa
      </Button>
    </Stack>
  );
}
