import { useEffect, useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  IconButton,
  List,
  ListItem,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';
import CloseIcon from '@mui/icons-material/Close';
import CasinoIcon from '@mui/icons-material/Casino';
import { GUID } from '@/modules/core/types/types';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { DrawMode, IDrawPreviewResult } from '@/modules/stage/type/stage';
import { confirmAction, notifyError, notifySuccess } from '@/modules/core/utils/confirmDialog';
import TeamLogo from '@/views/core/components/TeamLogo';

interface PlayoffDrawDialogProps {
  open: boolean;
  onClose: () => void;
  /** The first-round bracket stage to draw. */
  stageId: GUID;
  /** The division's roster — the pool of teams the draw seeds from. */
  roster: ITeamResponse[];
  /** Called after a successful commit so the caller can refetch the bracket. */
  onCommitted: () => void;
}

const teamName = (roster: ITeamResponse[], id: GUID): string =>
  roster.find(team => team.id === id)?.name ?? '(equipo desconocido)';

/**
 * Seeds a playoffs-only division's bracket (HU-128): a random draw goes
 * through a server-side preview-then-confirm flow so the previewed pairing is
 * guaranteed to equal the committed one (the draw token replays the exact
 * order); manual seeding lets the admin reorder the roster directly and
 * commits that exact order with no shuffle. Byes for a non-power-of-two
 * roster are handled entirely by the existing bracket seeder — this dialog
 * only ever produces an ordered team list.
 */
export default function PlayoffDrawDialog({
  open,
  onClose,
  stageId,
  roster,
  onCommitted,
}: PlayoffDrawDialogProps) {
  const { previewDraw, commitDraw } = useStage();

  const [mode, setMode] = useState<DrawMode>(DrawMode.Random);
  const [preview, setPreview] = useState<IDrawPreviewResult | null>(null);
  const [manualOrder, setManualOrder] = useState<GUID[]>([]);
  const [busy, setBusy] = useState(false);

  // Reset all draw-local state whenever the dialog (re)opens for a stage, so
  // a previous preview/order never leaks into the next open.
  useEffect(() => {
    if (!open) return;
    setMode(DrawMode.Random);
    setPreview(null);
    setManualOrder(roster.map(team => team.id));
  }, [open, stageId, roster]);

  const handlePreview = async () => {
    setBusy(true);
    try {
      const result = await previewDraw(stageId, { mode: DrawMode.Random });
      if (result) {
        setPreview(result);
      }
    } finally {
      setBusy(false);
    }
  };

  const handleConfirmRandom = async () => {
    if (!preview) return;

    const confirmed = await confirmAction({
      title: 'Confirmar sorteo',
      text: 'Esta acción sortea la llave con el emparejamiento mostrado. Se puede volver a sortear mientras ningún partido de esta llave se haya jugado.',
      confirmButtonText: 'Confirmar sorteo',
    });
    if (!confirmed) return;

    setBusy(true);
    try {
      const ok = await commitDraw(stageId, {
        mode: DrawMode.Random,
        drawToken: preview.drawToken,
      });
      if (ok) {
        await notifySuccess({ title: 'Sorteo realizado' });
        onCommitted();
        onClose();
      }
    } finally {
      setBusy(false);
    }
  };

  const handleConfirmManual = async () => {
    if (manualOrder.length < 2) {
      await notifyError({
        title: 'Faltan equipos',
        text: 'Se necesitan al menos 2 equipos inscriptos para sortear la llave.',
      });
      return;
    }

    const confirmed = await confirmAction({
      title: 'Confirmar sorteo manual',
      text: 'Esta acción arma la llave con el orden manual definido, sin ningún sorteo aleatorio.',
      confirmButtonText: 'Confirmar sorteo',
    });
    if (!confirmed) return;

    setBusy(true);
    try {
      const ok = await commitDraw(stageId, {
        mode: DrawMode.Manual,
        manualOrder,
      });
      if (ok) {
        await notifySuccess({ title: 'Llave armada' });
        onCommitted();
        onClose();
      }
    } finally {
      setBusy(false);
    }
  };

  const moveManual = (index: number, direction: -1 | 1) => {
    setManualOrder(prev => {
      const target = index + direction;
      if (target < 0 || target >= prev.length) return prev;
      const next = [...prev];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle sx={{ pr: 6 }}>
        Sortear llave
        <IconButton
          aria-label="Cerrar"
          onClick={onClose}
          sx={{ position: 'absolute', right: 8, top: 8 }}
        >
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers>
        <Tabs
          value={mode}
          onChange={(_e, value: DrawMode) => {
            setMode(value);
            setPreview(null);
          }}
          sx={{ mb: 2 }}
        >
          <Tab label="Aleatorio" value={DrawMode.Random} />
          <Tab label="Manual" value={DrawMode.Manual} />
        </Tabs>

        {mode === DrawMode.Random ? (
          <Stack spacing={2}>
            {!preview ? (
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Sortea un emparejamiento aleatorio entre los {roster.length} equipos
                inscriptos. Podés volver a sortear las veces que quieras antes de
                confirmar.
              </Typography>
            ) : (
              <List dense disablePadding>
                {preview.pairs.map((pair, index) => (
                  <ListItem key={`${pair.homeTeamId}-${index}`} disableGutters>
                    <Typography variant="body2">
                      {teamName(roster, pair.homeTeamId)} vs{' '}
                      {pair.visitorTeamId
                        ? teamName(roster, pair.visitorTeamId)
                        : 'BYE (pasa directo)'}
                    </Typography>
                  </ListItem>
                ))}
              </List>
            )}

            <Stack direction="row" spacing={1}>
              <Button
                variant={preview ? 'outlined' : 'contained'}
                startIcon={<CasinoIcon />}
                disabled={busy || roster.length < 2}
                onClick={() => void handlePreview()}
              >
                {preview ? 'Volver a sortear' : 'Sortear llave (aleatorio)'}
              </Button>
              {preview && (
                <Button
                  variant="contained"
                  disabled={busy}
                  onClick={() => void handleConfirmRandom()}
                >
                  Confirmar sorteo
                </Button>
              )}
            </Stack>
          </Stack>
        ) : (
          <Stack spacing={2}>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Ordená los equipos como quieras que queden sembrados en la llave — sin
              ningún sorteo aleatorio.
            </Typography>
            <List dense disablePadding>
              {manualOrder.map((teamId, index) => (
                <Box key={teamId}>
                  {index > 0 && <Divider component="li" />}
                  <ListItem
                    disableGutters
                    secondaryAction={
                      <Stack direction="row">
                        <IconButton
                          size="small"
                          aria-label={`Subir ${teamName(roster, teamId)}`}
                          disabled={busy || index === 0}
                          onClick={() => moveManual(index, -1)}
                        >
                          <ArrowUpwardIcon fontSize="small" />
                        </IconButton>
                        <IconButton
                          size="small"
                          aria-label={`Bajar ${teamName(roster, teamId)}`}
                          disabled={busy || index === manualOrder.length - 1}
                          onClick={() => moveManual(index, 1)}
                        >
                          <ArrowDownwardIcon fontSize="small" />
                        </IconButton>
                      </Stack>
                    }
                  >
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      <Typography variant="body2" sx={{ color: 'text.secondary', minWidth: 20 }}>
                        {index + 1}
                      </Typography>
                      <TeamLogo
                        teamName={teamName(roster, teamId)}
                        logoUrl={roster.find(t => t.id === teamId)?.logoUrl ?? ''}
                        size={24}
                      />
                      <Typography variant="body2">{teamName(roster, teamId)}</Typography>
                    </Stack>
                  </ListItem>
                </Box>
              ))}
            </List>

            <Button
              variant="contained"
              disabled={busy || manualOrder.length < 2}
              onClick={() => void handleConfirmManual()}
              sx={{ alignSelf: 'flex-start' }}
            >
              Confirmar sorteo
            </Button>
          </Stack>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          Cerrar
        </Button>
      </DialogActions>
    </Dialog>
  );
}
