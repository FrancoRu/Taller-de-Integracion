import { useEffect, useRef, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CircularProgress,
  Grid,
  LinearProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';

const STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programados',
  OpenForRegistration: 'Inscripción abierta',
  Ongoing: 'En curso',
  Finished: 'Finalizados',
  Canceled: 'Cancelados',
};

const STATUS_COLOR: Record<TournamentStatus, string> = {
  Scheduled: '#1976d2',
  OpenForRegistration: '#0288d1',
  Ongoing: '#2e7d32',
  Finished: '#616161',
  Canceled: '#d32f2f',
};

interface Summary {
  tournamentsTotal: number;
  tournamentsByStatus: Record<TournamentStatus, number>;
  teamsTotal: number;
  matchesPlayed: number;
  matchesScheduled: number;
  sanctionsTotal: number;
  topScorers: IScorerByPlayerResponse[];
}

const StatCard = ({
  label,
  value,
  color,
}: {
  label: string;
  value: number;
  color: string;
}) => (
  <Card sx={{ borderTop: `4px solid ${color}`, height: '100%' }}>
    <CardContent>
      <Typography variant="h3" fontWeight="bold">
        {value}
      </Typography>
      <Typography variant="body2" color="text.secondary">
        {label}
      </Typography>
    </CardContent>
  </Card>
);

const StatisticsPage = () => {
  const { getAllTournamentsByFilter } = useTournament();
  const { getTeamsByFiltered } = useTeam();
  const { getMatchByFilter } = useMatch();
  const { getScorersByPlayerFiltered } = useScorer();
  const { getPlayerSanctionByFilter } = usePlayerSanction();

  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<Summary | null>(null);

  const tournamentsRef = useRef(getAllTournamentsByFilter);
  const teamsRef = useRef(getTeamsByFiltered);
  const matchesRef = useRef(getMatchByFilter);
  const scorersRef = useRef(getScorersByPlayerFiltered);
  const sanctionsRef = useRef(getPlayerSanctionByFilter);

  useEffect(() => {
    tournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);
  useEffect(() => {
    teamsRef.current = getTeamsByFiltered;
  }, [getTeamsByFiltered]);
  useEffect(() => {
    matchesRef.current = getMatchByFilter;
  }, [getMatchByFilter]);
  useEffect(() => {
    scorersRef.current = getScorersByPlayerFiltered;
  }, [getScorersByPlayerFiltered]);
  useEffect(() => {
    sanctionsRef.current = getPlayerSanctionByFilter;
  }, [getPlayerSanctionByFilter]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);

      const [tournaments, teams, played, scheduled, sanctions, scorers] =
        await Promise.all([
          tournamentsRef.current({ pageSize: 100, pageNumber: 1 }),
          teamsRef.current({ pageSize: 1, pageNumber: 1 }),
          matchesRef.current({ pageSize: 1, pageNumber: 1, isFinished: true }),
          matchesRef.current({ pageSize: 1, pageNumber: 1, isFinished: false }),
          sanctionsRef.current({ pageSize: 1, pageNumber: 1 }),
          scorersRef.current({ pageSize: 5, pageNumber: 1 }),
        ]);

      const byStatus: Record<TournamentStatus, number> = {
        Scheduled: 0,
        OpenForRegistration: 0,
        Ongoing: 0,
        Finished: 0,
        Canceled: 0,
      };
      (tournaments?.items ?? []).forEach(t => {
        byStatus[t.status] = (byStatus[t.status] ?? 0) + 1;
      });

      setSummary({
        tournamentsTotal: tournaments?.totalCount ?? 0,
        tournamentsByStatus: byStatus,
        teamsTotal: teams?.totalCount ?? 0,
        matchesPlayed: played?.totalCount ?? 0,
        matchesScheduled: scheduled?.totalCount ?? 0,
        sanctionsTotal: sanctions?.totalCount ?? 0,
        topScorers: scorers?.items ?? [],
      });
      setLoading(false);
    };

    void load();
  }, []);

  if (loading || !summary) {
    return (
      <Box display="flex" justifyContent="center" py={10}>
        <CircularProgress />
      </Box>
    );
  }

  const matchesTotal = summary.matchesPlayed + summary.matchesScheduled;
  const playedPercent =
    matchesTotal === 0 ? 0 : (summary.matchesPlayed / matchesTotal) * 100;
  const maxScorerPoints = summary.topScorers[0]?.points ?? 0;

  return (
    <Box>
      <Typography variant="h5" fontWeight="bold" mb={3}>
        Estadísticas
      </Typography>

      <Grid container spacing={2} mb={1}>
        <Grid item xs={6} md={3}>
          <StatCard
            label="Torneos"
            value={summary.tournamentsTotal}
            color="#1976d2"
          />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard
            label="Equipos"
            value={summary.teamsTotal}
            color="#2e7d32"
          />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard
            label="Partidos jugados"
            value={summary.matchesPlayed}
            color="#ed6c02"
          />
        </Grid>
        <Grid item xs={6} md={3}>
          <StatCard
            label="Sanciones"
            value={summary.sanctionsTotal}
            color="#d32f2f"
          />
        </Grid>
      </Grid>

      <Grid container spacing={2} mt={1}>
        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" mb={2}>
                Torneos por estado
              </Typography>
              <Stack spacing={1.5}>
                {Object.entries(summary.tournamentsByStatus).map(
                  ([status, count]) => (
                    <Box
                      key={status}
                      display="flex"
                      alignItems="center"
                      justifyContent="space-between"
                    >
                      <Box display="flex" alignItems="center" gap={1}>
                        <Box
                          sx={{
                            width: 12,
                            height: 12,
                            borderRadius: '50%',
                            bgcolor: STATUS_COLOR[status as TournamentStatus],
                          }}
                        />
                        <Typography variant="body2">
                          {STATUS_LABEL[status as TournamentStatus]}
                        </Typography>
                      </Box>
                      <Typography variant="body2" fontWeight={600}>
                        {count}
                      </Typography>
                    </Box>
                  )
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" mb={2}>
                Partidos
              </Typography>
              <Stack spacing={2}>
                <Box>
                  <Box
                    display="flex"
                    justifyContent="space-between"
                    mb={0.5}
                  >
                    <Typography variant="body2">Jugados</Typography>
                    <Typography variant="body2" fontWeight={600}>
                      {summary.matchesPlayed} / {matchesTotal}
                    </Typography>
                  </Box>
                  <LinearProgress
                    variant="determinate"
                    value={playedPercent}
                    sx={{ height: 8, borderRadius: 1 }}
                  />
                </Box>
                <Box
                  display="flex"
                  justifyContent="space-between"
                >
                  <Typography variant="body2" color="text.secondary">
                    Programados
                  </Typography>
                  <Typography variant="body2" fontWeight={600}>
                    {summary.matchesScheduled}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" mb={2}>
                Top goleadores
              </Typography>
              {summary.topScorers.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  Sin datos de goleadores.
                </Typography>
              ) : (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>#</TableCell>
                      <TableCell>Jugador</TableCell>
                      <TableCell align="right">Pts</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {summary.topScorers.map((scorer, index) => (
                      <TableRow key={scorer.playerId}>
                        <TableCell>{index + 1}</TableCell>
                        <TableCell>
                          <Typography variant="body2">
                            {scorer.fullName}
                          </Typography>
                          <LinearProgress
                            variant="determinate"
                            value={
                              maxScorerPoints === 0
                                ? 0
                                : (scorer.points / maxScorerPoints) * 100
                            }
                            sx={{ height: 4, borderRadius: 1, mt: 0.5 }}
                          />
                        </TableCell>
                        <TableCell align="right">
                          <Typography variant="body2" fontWeight={600}>
                            {scorer.points}
                          </Typography>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default StatisticsPage;
