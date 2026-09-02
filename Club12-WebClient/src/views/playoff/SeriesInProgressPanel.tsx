import { useState } from 'react';
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { notifyError, notifySuccess } from '@/modules/core/utils/confirmDialog';
import { extractProblemDetail } from '@/modules/core/utils/problemDetails';
import FormButtons from '@/views/core/components/FormButtons';
import SectionHeading from '@/views/core/components/SectionHeading';

interface SeriesInProgressPanelProps {
  /** Every series of the division's playoff, decided or not — this panel filters to the undecided ones itself. */
  seriesById: Map<GUID, IMatchSeriesResponse>;
  /** Called after a game is successfully added, so the caller can refetch brackets/series. */
  onGameAdded: () => void;
}

const gameSummary = (series: IMatchSeriesResponse): string =>
  series.games
    .filter(game => game.isFinished)
    .map(game => `J${game.gameNumber} ${game.homeScore}-${game.visitorScore}`)
    .join(' · ') || 'Sin partidos jugados todavía';

/**
 * Lists every not-yet-decided best-of-N playoff series of the division, with
 * an "Agregar próximo partido" action per series — the missing piece that
 * let a series be created and auto-seeded (StageService) but never actually
 * played past game 1: MatchSeriesService.AddGameToSeriesAsync already
 * existed on the backend with no UI caller anywhere. Hidden entirely when
 * there are no series at all (BestOf=1 stages never create one).
 */
export default function SeriesInProgressPanel({
  seriesById,
  onGameAdded,
}: SeriesInProgressPanelProps) {
  const [activeSeries, setActiveSeries] = useState<IMatchSeriesResponse | null>(null);
  const [matchDate, setMatchDate] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const undecidedSeries = [...seriesById.values()].filter(series => !series.winningTeamId);

  if (undecidedSeries.length === 0) {
    return null;
  }

  const handleClose = () => {
    if (submitting) {
      return;
    }
    setActiveSeries(null);
    setMatchDate('');
  };

  const handleConfirm = async () => {
    if (!activeSeries || !matchDate) {
      return;
    }

    setSubmitting(true);
    try {
      await matchSeriesService.addGameToSeries(activeSeries.id, {
        matchDate: new Date(matchDate).toISOString(),
      });
      setActiveSeries(null);
      setMatchDate('');
      onGameAdded();
      await notifySuccess({
        title: 'Partido agregado',
        text: 'Se agregó el próximo partido de la serie.',
      });
    } catch (error) {
      await notifyError({
        title: 'No se pudo agregar el partido',
        text: extractProblemDetail(error) ?? 'Intentá nuevamente.',
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box>
      <SectionHeading>Series en curso</SectionHeading>
      <Stack spacing={1.5}>
        {undecidedSeries.map(series => {
          const maxGamesReached = series.games.length >= series.bestOf;

          return (
            <Box
              key={series.id}
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 2,
                p: 1.5,
                border: 1,
                borderColor: 'divider',
                borderRadius: 1,
                flexWrap: 'wrap',
              }}
            >
              <Box sx={{ minWidth: 0 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {series.homeTeamName} vs {series.visitorTeamName} — al mejor de {series.bestOf}
                </Typography>
                <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                  {gameSummary(series)}
                </Typography>
              </Box>
              <Button
                variant="outlined"
                size="small"
                disabled={maxGamesReached}
                onClick={() => setActiveSeries(series)}
              >
                Agregar próximo partido
              </Button>
            </Box>
          );
        })}
      </Stack>

      <Dialog open={Boolean(activeSeries)} onClose={handleClose} fullWidth maxWidth="xs">
        <DialogTitle>Agregar próximo partido</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {activeSeries && (
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                {activeSeries.homeTeamName} vs {activeSeries.visitorTeamName} · Juego{' '}
                {activeSeries.games.length + 1} de hasta {activeSeries.bestOf}
              </Typography>
            )}
            <TextField
              label="Fecha y hora"
              type="datetime-local"
              value={matchDate}
              onChange={e => setMatchDate(e.target.value)}
              fullWidth
              required
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={handleClose}
            onConfirm={() => void handleConfirm()}
            confirmLabel="Agregar"
            disabled={submitting || !matchDate}
          />
        </DialogActions>
      </Dialog>
    </Box>
  );
}
