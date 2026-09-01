import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Chip,
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
import { IMatchResponse } from '@/modules/match/type/match';
import { useMatch } from '@/modules/match/hook/match.hook';
import { ITeamMatchResponse } from '@/modules/team/type/team';
import { IPublicPlayerResponse } from '@/modules/player/type/player.d';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { PlayerStatisticResponse } from '@/modules/playerStatistic/type/playerStatistic';
import { TableSkeleton } from '@/views/core/components/skeletons';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import HabilitacionBadge from '@/views/medicalRecord/HabilitacionBadge';
import { resolveIsHabilitado } from '@/modules/medicalRecord/utils/medicalRecordDisplay';

interface MatchStatisticsTabProps {
  match: IMatchResponse;
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

const buildFormFromPoints = (
  players: IPublicPlayerResponse[],
  pointsByPlayer: Record<string, number>
): Record<string, string> =>
  players.reduce<Record<string, string>>((acc, player) => {
    acc[player.id] = String(pointsByPlayer[player.id] ?? 0);
    return acc;
  }, {});

const sumForm = (form: Record<string, string>): number =>
  Object.values(form).reduce((total, value) => total + (Number(value) || 0), 0);

/**
 * Loads a match's result by entering each player's points for BOTH teams in
 * one place (HU-72): the final score is derived as the sum of what each
 * team's players scored, instead of being typed in separately and then
 * checked against a sheet loaded here afterward. This is the only place a
 * match's result is loaded — the score is always what the players add up to.
 */
export default function MatchStatisticsTab({ match }: MatchStatisticsTabProps) {
  const { loadMatchResultFromSheets } = useMatch();
  const { getPlayerStatisticsByFilter } = usePlayerStatistic();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [homeForm, setHomeForm] = useState<Record<string, string>>({});
  const [visitorForm, setVisitorForm] = useState<Record<string, string>>({});

  const getStatisticsRef = useRef(getPlayerStatisticsByFilter);
  useEffect(() => {
    getStatisticsRef.current = getPlayerStatisticsByFilter;
  }, [getPlayerStatisticsByFilter]);

  const homePlayers = useMemo(() => match.homeTeam?.players ?? [], [match.homeTeam]);
  const visitorPlayers = useMemo(
    () => match.visitorTeam?.players ?? [],
    [match.visitorTeam]
  );

  const loadStatistics = useCallback(async () => {
    setLoading(true);
    const response = await getStatisticsRef.current({
      matchId: match.id,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
    const items = response?.items ?? [];

    const pointsByPlayer = buildPointsMap(items);
    setHomeForm(buildFormFromPoints(homePlayers, pointsByPlayer));
    setVisitorForm(buildFormFromPoints(visitorPlayers, pointsByPlayer));
    setLoading(false);
    // homePlayers/visitorPlayers are derived from `match` on every render;
    // re-running this whenever the match's own id changes is enough and
    // avoids re-fetching on every unrelated match update.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [match.id]);

  useEffect(() => {
    void loadStatistics();
  }, [loadStatistics]);

  const homeSum = useMemo(() => sumForm(homeForm), [homeForm]);
  const visitorSum = useMemo(() => sumForm(visitorForm), [visitorForm]);
  const isTie = homeSum === visitorSum;
  const rostersReady = homePlayers.length > 0 && visitorPlayers.length > 0;

  const handleSaveResult = useCallback(async () => {
    if (!rostersReady || isTie) {
      return;
    }

    setSubmitting(true);
    const result = await loadMatchResultFromSheets(match.id, {
      homeScores: homePlayers.map(player => ({
        playerId: player.id,
        points: Number(homeForm[player.id]) || 0,
      })),
      visitorScores: visitorPlayers.map(player => ({
        playerId: player.id,
        points: Number(visitorForm[player.id]) || 0,
      })),
    });
    setSubmitting(false);

    // A falsy result means the backend rejected the sheets (e.g. a tied sum,
    // an ineligible player, or a 409); the message is surfaced globally and
    // the form stays as-is so the operator can correct it.
    if (!result) {
      return;
    }

    await loadStatistics();
    await notifySuccess({ title: 'Resultado cargado' });
  }, [
    rostersReady,
    isTie,
    loadMatchResultFromSheets,
    match.id,
    homePlayers,
    visitorPlayers,
    homeForm,
    visitorForm,
    loadStatistics,
  ]);

  const renderTeamCard = (
    team: ITeamMatchResponse | null,
    fallbackLabel: string,
    players: IPublicPlayerResponse[],
    form: Record<string, string>,
    setForm: React.Dispatch<React.SetStateAction<Record<string, string>>>,
    sum: number
  ) => (
    <Card variant="outlined">
      <CardContent>
        <Stack
          direction="row"
          sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}
        >
          <Typography variant="subtitle1">
            {team?.name || fallbackLabel}
          </Typography>
          <Chip size="small" label={`Suma: ${sum}`} />
        </Stack>

        {players.length === 0 ? (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Sin jugadores registrados. No se puede cargar un resultado hasta
            que el equipo tenga jugadores en el plantel.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Jugador</TableCell>
                <TableCell align="center">Puntos</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {players.map(player => (
                <TableRow key={player.id}>
                  <TableCell>
                    <Stack
                      direction="row"
                      spacing={1}
                      sx={{ alignItems: 'center' }}
                    >
                      <span>{player.fullName}</span>
                      {!resolveIsHabilitado(
                        player.isHabilitado,
                        player.medicalRecordStatus
                      ) && (
                        <HabilitacionBadge
                          isHabilitado={player.isHabilitado}
                          status={player.medicalRecordStatus}
                        />
                      )}
                    </Stack>
                  </TableCell>
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
        )}
      </CardContent>
    </Card>
  );

  if (loading) {
    return <TableSkeleton rows={5} columns={5} />;
  }

  return (
    <Stack spacing={2}>
      <Typography variant="body1">
        El resultado del partido se calcula sumando los puntos de cada
        jugador. Cargá la planilla de ambos equipos y guardá para finalizar
        el partido.
      </Typography>

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', md: 'repeat(2, 1fr)' },
        }}
      >
        {renderTeamCard(
          match.homeTeam,
          'Equipo local',
          homePlayers,
          homeForm,
          setHomeForm,
          homeSum
        )}
        {renderTeamCard(
          match.visitorTeam,
          'Equipo visitante',
          visitorPlayers,
          visitorForm,
          setVisitorForm,
          visitorSum
        )}
      </Box>

      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
      >
        <Typography variant="body2">
          Local: <strong>{homeSum}</strong> — Visitante:{' '}
          <strong>{visitorSum}</strong>
        </Typography>
        {isTie && (
          <Typography variant="body2" color="error">
            No se permiten empates: el partido debe tener un ganador.
          </Typography>
        )}
        {!rostersReady && (
          <Typography variant="body2" color="error">
            Ambos equipos necesitan jugadores en el plantel para cargar el
            resultado.
          </Typography>
        )}
      </Stack>

      <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
        <Button
          variant="contained"
          onClick={() => void handleSaveResult()}
          disabled={submitting || isTie || !rostersReady}
        >
          Guardar resultado
        </Button>
      </Box>
    </Stack>
  );
}
