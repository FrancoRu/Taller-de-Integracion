import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
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
import { IPublicPlayerResponse } from '@/modules/player/type/player.d';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import {
  PlayerStatisticResponse,
  StatisticType,
} from '@/modules/playerStatistic/type/playerStatistic';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

interface MatchStatisticsTabProps {
  match: IMatchResponse;
}

interface StatRecord {
  id: GUID;
  value: number;
}

interface FormRow {
  points: string;
  assists: string;
}

const buildStatMap = (
  statistics: PlayerStatisticResponse[],
  type: StatisticType
): Record<string, StatRecord> =>
  statistics
    .filter(stat => stat.type === type)
    .reduce<Record<string, StatRecord>>((acc, stat) => {
      acc[stat.playerId] = { id: stat.id, value: stat.value };
      return acc;
    }, {});

export default function MatchStatisticsTab({ match }: MatchStatisticsTabProps) {
  const {
    getPlayerStatisticsByFilter,
    addPlayerStatistic,
    putPlayerStatisticById,
    deletePlayerStatisticById,
  } = usePlayerStatistic();

  const [statistics, setStatistics] = useState<PlayerStatisticResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<Record<string, FormRow>>({});

  const getStatisticsRef = useRef(getPlayerStatisticsByFilter);
  useEffect(() => {
    getStatisticsRef.current = getPlayerStatisticsByFilter;
  }, [getPlayerStatisticsByFilter]);

  const players = useMemo<IPublicPlayerResponse[]>(
    () => [
      ...(match.homeTeam?.players ?? []),
      ...(match.visitorTeam?.players ?? []),
    ],
    [match.homeTeam?.players, match.visitorTeam?.players]
  );

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

  const pointsByPlayer = useMemo(
    () => buildStatMap(statistics, 'Points'),
    [statistics]
  );
  const assistsByPlayer = useMemo(
    () => buildStatMap(statistics, 'Assists'),
    [statistics]
  );

  const openDialog = useCallback(() => {
    const initialForm: Record<string, FormRow> = {};
    players.forEach(player => {
      initialForm[player.id] = {
        points: String(pointsByPlayer[player.id]?.value ?? 0),
        assists: String(assistsByPlayer[player.id]?.value ?? 0),
      };
    });
    setForm(initialForm);
    setDialogOpen(true);
  }, [assistsByPlayer, players, pointsByPlayer]);

  const closeDialog = useCallback(() => {
    if (submitting) {
      return;
    }
    setDialogOpen(false);
  }, [submitting]);

  const persistStat = useCallback(
    async (
      playerId: GUID,
      type: StatisticType,
      newValue: number,
      existing?: StatRecord
    ) => {
      if (existing) {
        if (newValue === existing.value) {
          return;
        }
        if (newValue <= 0) {
          await deletePlayerStatisticById(existing.id);
          return;
        }
        await putPlayerStatisticById(existing.id, { value: newValue });
        return;
      }

      if (newValue > 0) {
        await addPlayerStatistic({
          value: newValue,
          matchId: match.id,
          playerId,
          type,
        });
      }
    },
    [addPlayerStatistic, deletePlayerStatisticById, match.id, putPlayerStatisticById]
  );

  const handleSave = useCallback(async () => {
    setSubmitting(true);

    for (const player of players) {
      const row = form[player.id];
      if (!row) {
        continue;
      }

      const points = Number(row.points) || 0;
      const assists = Number(row.assists) || 0;

      await persistStat(player.id, 'Points', points, pointsByPlayer[player.id]);
      await persistStat(
        player.id,
        'Assists',
        assists,
        assistsByPlayer[player.id]
      );
    }

    setSubmitting(false);
    setDialogOpen(false);
    await loadStatistics();
    await notifySuccess({ title: 'Puntuaciones guardadas' });
  }, [
    assistsByPlayer,
    form,
    loadStatistics,
    persistStat,
    players,
    pointsByPlayer,
  ]);

  const renderTeam = (
    team: IMatchResponse['homeTeam'],
    fallbackLabel: string
  ) => {
    const teamPlayers = team?.players ?? [];

    return (
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" mb={1}>
            {team?.name || fallbackLabel}
          </Typography>
          {teamPlayers.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Sin jugadores registrados.
            </Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Jugador</TableCell>
                  <TableCell align="center">Goles</TableCell>
                  <TableCell align="center">Asistencias</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {teamPlayers.map(player => (
                  <TableRow key={player.id}>
                    <TableCell>{player.fullName}</TableCell>
                    <TableCell align="center">
                      {pointsByPlayer[player.id]?.value ?? 0}
                    </TableCell>
                    <TableCell align="center">
                      {assistsByPlayer[player.id]?.value ?? 0}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    );
  };

  if (loading) {
    return <LoadingIndicator />;
  }

  return (
    <Stack spacing={2}>
      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="center"
      >
        <Typography variant="body1">
          Goles y asistencias por jugador.
        </Typography>
        <Button
          variant="contained"
          onClick={openDialog}
          disabled={players.length === 0}
        >
          Cargar puntuaciones
        </Button>
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
        }}
      >
        {renderTeam(match.homeTeam, 'Equipo local')}
        {renderTeam(match.visitorTeam, 'Equipo visitante')}
      </Box>

      <Dialog open={dialogOpen} onClose={closeDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Cargar puntuaciones</DialogTitle>
        <DialogContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Jugador</TableCell>
                <TableCell align="center">Goles</TableCell>
                <TableCell align="center">Asistencias</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {players.map(player => (
                <TableRow key={player.id}>
                  <TableCell>{player.fullName}</TableCell>
                  <TableCell align="center">
                    <TextField
                      type="number"
                      size="small"
                      value={form[player.id]?.points ?? '0'}
                      onChange={e =>
                        setForm(prev => ({
                          ...prev,
                          [player.id]: {
                            points: e.target.value,
                            assists: prev[player.id]?.assists ?? '0',
                          },
                        }))
                      }
                      inputProps={{ min: 0, style: { width: 64 } }}
                    />
                  </TableCell>
                  <TableCell align="center">
                    <TextField
                      type="number"
                      size="small"
                      value={form[player.id]?.assists ?? '0'}
                      onChange={e =>
                        setForm(prev => ({
                          ...prev,
                          [player.id]: {
                            points: prev[player.id]?.points ?? '0',
                            assists: e.target.value,
                          },
                        }))
                      }
                      inputProps={{ min: 0, style: { width: 64 } }}
                    />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          <Stack direction="row" justifyContent="flex-end" spacing={1} mt={2}>
            <Button onClick={closeDialog} disabled={submitting} color="inherit">
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void handleSave()}
              disabled={submitting}
            >
              Guardar
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Stack>
  );
}
