import { Box, Button, IconButton, MenuItem, Stack, TextField, Typography } from '@mui/material';
import { AddIcon, DeleteIcon } from '@/views/core/MUI/icons/icons';
import { StageType } from '@/modules/stage/type/stage';
import {
  BEST_OF_OPTIONS,
  CupConfig,
  ELIMINATION_STAGE_TYPES,
  STAGE_TYPE_LABELS,
  createEmptyCup,
  createEmptyRound,
} from '../types';

interface CupsEditorProps {
  cups: CupConfig[];
  onChange: (cups: CupConfig[]) => void;
}

/**
 * Lets the admin build zero or more parallel playoff cups for a zone (or
 * the cross-division cup): each with a free-text name and an ordered list
 * of rounds, each round with its own stage type and best-of format.
 * "Copa de Oro" / "Copa de Plata" are never suggested here — the admin
 * types whatever name they want, or none at all.
 */
export default function CupsEditor({ cups, onChange }: CupsEditorProps) {
  const updateCup = (cupId: string, updates: Partial<CupConfig>) => {
    onChange(cups.map(cup => (cup.id === cupId ? { ...cup, ...updates } : cup)));
  };

  const addCup = () => onChange([...cups, createEmptyCup()]);
  const removeCup = (cupId: string) => onChange(cups.filter(cup => cup.id !== cupId));

  const addRound = (cupId: string) => {
    const cup = cups.find(c => c.id === cupId);
    if (!cup) return;
    updateCup(cupId, { rounds: [...cup.rounds, createEmptyRound()] });
  };

  const removeRound = (cupId: string, roundId: string) => {
    const cup = cups.find(c => c.id === cupId);
    if (!cup) return;
    updateCup(cupId, { rounds: cup.rounds.filter(round => round.id !== roundId) });
  };

  return (
    <Stack spacing={2}>
      {cups.map(cup => (
        <Box key={cup.id} sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: 2 }}>
          <Stack
            direction="row"
            spacing={1}
            sx={{
              alignItems: "center",
              mb: 1.5
            }}>
            <TextField
              label="Nombre de la copa (libre)"
              size="small"
              value={cup.name}
              onChange={e => updateCup(cup.id, { name: e.target.value })}
              fullWidth
            />
            <IconButton
              aria-label="Eliminar copa"
              color="error"
              onClick={() => removeCup(cup.id)}
            >
              <DeleteIcon fontSize="small" />
            </IconButton>
          </Stack>

          <Stack spacing={1}>
            {cup.rounds.map(round => (
              <Stack key={round.id} direction="row" spacing={1} sx={{
                alignItems: "center"
              }}>
                <TextField
                  select
                  size="small"
                  label="Ronda"
                  value={round.stageType}
                  onChange={e =>
                    updateCup(cup.id, {
                      rounds: cup.rounds.map(r =>
                        r.id === round.id
                          ? { ...r, stageType: e.target.value as StageType }
                          : r
                      ),
                    })
                  }
                  sx={{ minWidth: 180 }}
                >
                  {ELIMINATION_STAGE_TYPES.map(stageType => (
                    <MenuItem key={stageType} value={stageType}>
                      {STAGE_TYPE_LABELS[stageType]}
                    </MenuItem>
                  ))}
                </TextField>

                <TextField
                  select
                  size="small"
                  label="Formato"
                  value={round.bestOf}
                  onChange={e =>
                    updateCup(cup.id, {
                      rounds: cup.rounds.map(r =>
                        r.id === round.id ? { ...r, bestOf: Number(e.target.value) } : r
                      ),
                    })
                  }
                  sx={{ minWidth: 160 }}
                >
                  {BEST_OF_OPTIONS.map(option => (
                    <MenuItem key={option} value={option}>
                      {option === 1 ? 'Partido único' : `Al mejor de ${option}`}
                    </MenuItem>
                  ))}
                </TextField>

                <IconButton
                  aria-label="Eliminar ronda"
                  size="small"
                  onClick={() => removeRound(cup.id, round.id)}
                >
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}

            <Button
              size="small"
              startIcon={<AddIcon />}
              onClick={() => addRound(cup.id)}
              sx={{ alignSelf: 'flex-start' }}
            >
              Agregar ronda
            </Button>
          </Stack>
        </Box>
      ))}

      {cups.length === 0 && (
        <Typography variant="body2" sx={{
          color: "text.secondary"
        }}>
          Sin copas configuradas. Podés agregar una o más — cada una con su propio nombre libre y formato por ronda.
        </Typography>
      )}

      <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={addCup} sx={{ alignSelf: 'flex-start' }}>
        Agregar copa
      </Button>
    </Stack>
  );
}
