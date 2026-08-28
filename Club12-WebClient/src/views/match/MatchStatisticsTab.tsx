import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import { notifySuccess } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IPublicPlayerResponse } from '@/modules/player/type/player.d';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import {
  PlayerScoreEntry,
  PlayerStatisticResponse,
} from '@/modules/playerStatistic/type/playerStatistic';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

interface MatchStatisticsTabProps {
  match: IMatchResponse;
}

/** The team currently being edited in the sheet dialog. */
interface ActiveTeam {
  id: GUID;
  name: string;
  score: number;
  players: IPublicPlayerResponse[];
}

/**
 * Sums the Points statistics for a match, keyed by player id, so each team's
 * currently-loaded planilla can be shown without a second request.
 */
const buildPointsMap = (
  statistics: PlayerStatisticResponse[]
): Record<string, number> =>
  statistics
    .filter(stat => stat.type === 'Points')
    .reduce<Record<string, number>>((acc, stat) => {
      acc[stat.playerId] = (acc[stat.playerId] ?? 0) + stat.value;
      return acc;
    }, {});

const toActiveTeam = (team: ITeamMatchResponse | null): ActiveTeam | null =>
  team
    ? {
        id: team.id,
        name: team.name,
        score: team.score ?? 0,
        players: team.players ?? [],
      }
    : null;

export default function MatchStatisticsTab({ match }: MatchStatisticsTabProps) {
  const { getPlayerStatisticsByFilter, loadMatchSheet } = usePlayerStatistic();

  const [statistics, setStatistics] = useState<PlayerStatisticResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [activeTeam, setActiveTeam] = useState<ActiveTeam | null>(null);
  const [form, setForm] = useState<Record<string, string>>({});

  const getStatisticsRef = useRef(getPlayerStatisticsByFilter);
  useEffect(() => {
    getStatisticsRef.current = getPlayerStatisticsByFilter;
  }, [getPlayerStatisticsByFilter]);

  const loadStatistics = useCallback(async () => {
    setLoading(true);
    const response = await getStatisticsRef.current({
      matchId: match.id,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
    setStatistics(response?.items ?? []);
    setLoading(false);
  }, [match.id]);

  useEffect(() => {
    void loadStatistics();
  }, [loadStatistics]);

  const pointsByPlayer = useMemo(() => buildPointsMap(statistics), [statistics]);

  const openDialog = useCallback((team: ActiveTeam) => {
    const initialForm: Record<string, string> = {};
    team.players.forEach(player => {
      initialForm[player.id] = '0';
    });
    setForm(initialForm);
    setActiveTeam(team);
  }, []);

  const closeDialog = useCallback(() => {
    if (submitting) {
      return;
    }
    setActiveTeam(null);
  }, [submitting]);

  const currentSum = useMemo(
    () =>
      Object.values(form).reduce(
        (total, value) => total + (Number(value) || 0),
        0
      ),
    [form]
  );

  const targetScore = activeTeam?.score ?? 0;
  const difference = currentSum - targetScore;
  const sumMatches = difference === 0;

  const handleSave = useCallback(async () => {
    if (!activeTeam || !sumMatches) {
      return;
    }

    const scores: PlayerScoreEntry[] = activeTeam.players.map(player => ({
      playerId: player.id,
      points: Number(form[player.id]) || 0,
    }));

    setSubmitting(true);
    const result = await loadMatchSheet({
      matchId: match.id,
      teamId: activeTeam.id,
      scores,
    });
    setSubmitting(false);

    // A falsy result means the backend rejected the sheet (e.g. a 409 sum
    // mismatch or an ineligible player); the message is surfaced globally and
    // the dialog stays open so the operator can correct it.
    if (!result) {
      return;
    }

    setActiveTeam(null);
    await loadStatistics();
    await notifySuccess({ title: 'Planilla cargada' });
  }, [activeTeam, form, loadMatchSheet, loadStatistics, match.id, sumMatches]);

  const renderTeamCard = (
    team: ITeamMatchResponse | null,
    fallbackLabel: string
  ) => {
    const activeCandidate = toActiveTeam(team);
    const teamPlayers = team?.players ?? [];
    const loadedTotal = teamPlayers.reduce(
      (total, player) => total + (pointsByPlayer[player.id] ?? 0),
      0
    );

    return (
      <Card variant="outlined">
        <CardContent>
          <Stack
            direction="row"
            sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}
          >
            <Typography variant="subtitle1">
              {team?.name || fallbackLabel}
            </Typography>
            <Chip
              size="small"
              label={`Marcador: ${team?.score ?? 0}`}
              color={loadedTotal === (team?.score ?? 0) ? 'success' : 'default'}
            />
          </Stack>

          {teamPlayers.length === 0 ? (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Sin jugadores registrados.
            </Typography>
          ) : (
            <>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Jugador</TableCell>
                    <TableCell align="center">Puntos</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {teamPlayers.map(player => (
                    <TableRow key={player.id}>
                      <TableCell>{player.fullName}</TableCell>
                      <TableCell align="center">
                        {pointsByPlayer[player.id] ?? 0}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
              <Typography
                variant="caption"
                sx={{ color: 'text.secondary', mt: 1, display: 'block' }}
              >
                Cargado: {loadedTotal} / {team?.score ?? 0}
              </Typography>
            </>
          )}

          <Box sx={{ mt: 2 }}>
            <Button
              variant="contained"
              size="small"
              onClick={() =>
                activeCandidate && openDialog(activeCandidate)
              }
              disabled={teamPlayers.length === 0}
            >
              Cargar planilla
            </Button>
          </Box>
        </CardContent>
      </Card>
    );
  };

  if (loading) {
    return <LoadingIndicator />;
  }

  return (
    <Stack spacing={2}>
      <Typography variant="body1">
        Planilla de puntos por jugador. La suma de cada equipo debe coincidir
        con su marcador.
      </Typography>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
        }}
      >
        {renderTeamCard(match.homeTeam, 'Equipo local')}
        {renderTeamCard(match.visitorTeam, 'Equipo visitante')}
      </Box>

      <Dialog
        open={Boolean(activeTeam)}
        onClose={closeDialog}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Planilla — {activeTeam?.name}</DialogTitle>
        <DialogContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Jugador</TableCell>
                <TableCell align="center">Puntos</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {(activeTeam?.players ?? []).map(player => (
                <TableRow key={player.id}>
                  <TableCell>{player.fullName}</TableCell>
                  <TableCell align="center">
                    <TextField
                      type="number"
                      size="small"
                      value={form[player.id] ?? '0'}
                      onChange={e =>
                        setForm(prev => ({
                          ...prev,
                          [player.id]: e.target.value,
                        }))
                      }
                      slotProps={{
                        htmlInput: {
                          min: 0,
                          style: { width: 72 },
                          'aria-label': `Puntos de ${player.fullName}`,
                        },
                      }}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <Stack
            direction="row"
            spacing={2}
            sx={{ justifyContent: 'space-between', alignItems: 'center', mt: 2 }}
          >
            <Typography variant="body2">
              Suma: <strong>{currentSum}</strong> / Marcador:{' '}
              <strong>{targetScore}</strong>
            </Typography>
            {!sumMatches && (
              <Typography variant="body2" color="error">
                {difference > 0
                  ? `Sobran ${difference} puntos`
                  : `Faltan ${Math.abs(difference)} puntos`}
              </Typography>
            )}
          </Stack>

          <Stack
            direction="row"
            spacing={1}
            sx={{ justifyContent: 'flex-end', mt: 2 }}
          >
            <Button onClick={closeDialog} disabled={submitting} color="inherit">
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void handleSave()}
              disabled={submitting || !sumMatches}
            >
              Guardar planilla
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
