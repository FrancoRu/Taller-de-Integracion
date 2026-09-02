import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Grid,
  LinearProgress,
  MenuItem,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import { CardGridSkeleton, TableSkeleton } from '@/views/core/components/skeletons';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { useSeason } from '@/modules/season/hook/season.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { GUID } from '@/modules/core/types/types';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { buildScorerScopeParams } from '@/modules/scorer/utils/scorerScope';
import {
  deriveTournamentOptions,
  resolveScopeTournamentIds,
  resolveSeasonYear,
} from '@/views/panel/statisticsFilters';

/** Rows fetched for the top goleadores ranking card. */
const TOP_SCORERS_COUNT = 5;

const STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programados',
  OpenForRegistration: 'Inscripción abierta',
  RegistrationClosed: 'Inscripción cerrada',
  Ongoing: 'En curso',
  Finished: 'Finalizados',
  Canceled: 'Cancelados',
};

const STATUS_COLOR: Record<TournamentStatus, string> = {
  Scheduled: '#1976d2',
  OpenForRegistration: '#0288d1',
  RegistrationClosed: '#9c27b0',
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
      <Typography variant="h3" sx={{
        fontWeight: "bold"
      }}>
        {value}
      </Typography>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        {label}
      </Typography>
    </CardContent>
  </Card>
);

const StatisticsPage = () => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { getTeamsByFiltered } = useTeam();
  const { getMatchByFilter } = useMatch();
  const { getScorersByPlayerFiltered } = useScorer();
  const { seasons, getSeasonsByFiltered } = useSeason();
  const { getPlayerSanctionByFilter } = usePlayerSanction();

  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<Summary | null>(null);

  // Scope filter (HU): a temporada and/or a torneo. Both empty = global stats,
  // preserving the original behaviour.
  const [selectedSeasonId, setSelectedSeasonId] = useState<GUID | ''>('');
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>(
    ''
  );

  const [topScorers, setTopScorers] = useState<IScorerByPlayerResponse[]>([]);
  const [scorersLoading, setScorersLoading] = useState(true);

  const tournamentsRef = useRef(getAllTournamentsByFilter);
  const teamsRef = useRef(getTeamsByFiltered);
  const matchesRef = useRef(getMatchByFilter);
  const scorersRef = useRef(getScorersByPlayerFiltered);
  const seasonsRef = useRef(getSeasonsByFiltered);
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
    seasonsRef.current = getSeasonsByFiltered;
  }, [getSeasonsByFiltered]);
  useEffect(() => {
    sanctionsRef.current = getPlayerSanctionByFilter;
  }, [getPlayerSanctionByFilter]);

  // Which tournaments the summary cards are scoped to: null (unscoped/
  // global) with neither filter picked, otherwise the chosen torneo alone
  // or every tournament the chosen temporada groups.
  const scopeTournamentIds = useMemo(
    () => resolveScopeTournamentIds(seasons, selectedSeasonId, selectedTournamentId),
    [seasons, selectedSeasonId, selectedTournamentId]
  );

  // Loads the summary cards and the filter option sources (tournaments +
  // seasons), scoped by `scopeTournamentIds`. Unscoped shows the club-wide
  // totals; otherwise every card — not just goleadores — reflects only the
  // chosen torneo/temporada.
  useEffect(() => {
    const load = async () => {
      setLoading(true);

      const [tournamentsPage] = await Promise.all([
        tournamentsRef.current({
          pageSize: FILTER_OPTIONS_PAGE_SIZE,
          pageNumber: 1,
        }),
        seasonsRef.current({}),
      ]);

      const scopedTournaments =
        scopeTournamentIds === null
          ? (tournamentsPage?.items ?? [])
          : (tournamentsPage?.items ?? []).filter(t =>
              scopeTournamentIds.includes(t.id)
            );

      const byStatus: Record<TournamentStatus, number> = {
        Scheduled: 0,
        OpenForRegistration: 0,
        RegistrationClosed: 0,
        Ongoing: 0,
        Finished: 0,
        Canceled: 0,
      };
      scopedTournaments.forEach(t => {
        byStatus[t.status] = (byStatus[t.status] ?? 0) + 1;
      });

      // Runs one global call when unscoped, or one call per scoped
      // tournament (summing their totals) when a torneo/temporada is chosen
      // — teamsRef/matchesRef/sanctionsRef only filter by a single
      // tournamentId, not a season, so a season's scope is the sum across
      // every tournament it groups.
      const sumScoped = async (
        fetchByTournament: (
          tournamentId: GUID
        ) => Promise<{ totalCount: number } | void>,
        fetchGlobal: () => Promise<{ totalCount: number } | void>
      ): Promise<number> => {
        if (scopeTournamentIds === null) {
          const result = await fetchGlobal();
          return result?.totalCount ?? 0;
        }
        const results = await Promise.all(scopeTournamentIds.map(fetchByTournament));
        return results.reduce((sum, r) => sum + (r?.totalCount ?? 0), 0);
      };

      const [teamsTotal, matchesPlayed, matchesScheduled, sanctionsTotal] =
        await Promise.all([
          sumScoped(
            tournamentId =>
              teamsRef.current({ tournamentId, pageSize: 1, pageNumber: 1 }),
            () => teamsRef.current({ pageSize: 1, pageNumber: 1 })
          ),
          sumScoped(
            tournamentId =>
              matchesRef.current({
                tournamentId,
                pageSize: 1,
                pageNumber: 1,
                isFinished: true,
              }),
            () => matchesRef.current({ pageSize: 1, pageNumber: 1, isFinished: true })
          ),
          sumScoped(
            tournamentId =>
              matchesRef.current({
                tournamentId,
                pageSize: 1,
                pageNumber: 1,
                isFinished: false,
              }),
            () =>
              matchesRef.current({ pageSize: 1, pageNumber: 1, isFinished: false })
          ),
          sumScoped(
            tournamentId =>
              sanctionsRef.current({ tournamentId, pageSize: 1, pageNumber: 1 }),
            () => sanctionsRef.current({ pageSize: 1, pageNumber: 1 })
          ),
        ]);

      setSummary({
        tournamentsTotal:
          scopeTournamentIds === null
            ? (tournamentsPage?.totalCount ?? 0)
            : scopedTournaments.length,
        tournamentsByStatus: byStatus,
        teamsTotal,
        matchesPlayed,
        matchesScheduled,
        sanctionsTotal,
      });
      setLoading(false);
    };

    void load();
  }, [scopeTournamentIds]);

  const tournamentOptions = useMemo(
    () => deriveTournamentOptions(seasons, selectedSeasonId, tournaments),
    [seasons, selectedSeasonId, tournaments]
  );

  // The goleadores scope resolved to Scorer/by-player query params: a chosen
  // torneo wins (most specific); otherwise the chosen temporada's calendar
  // year; otherwise the all-time ranking.
  const scorerScopeParams = useMemo(() => {
    const seasonYear = resolveSeasonYear(seasons, selectedSeasonId);
    const scope = selectedTournamentId
      ? 'tournament'
      : seasonYear !== ''
        ? 'season'
        : 'allTime';
    return buildScorerScopeParams(scope, {
      tournamentId: selectedTournamentId,
      season: seasonYear,
    });
  }, [seasons, selectedSeasonId, selectedTournamentId]);

  const isScoped = selectedSeasonId !== '' || selectedTournamentId !== '';

  // Reload the goleadores ranking whenever the scope changes.
  useEffect(() => {
    const loadScorers = async () => {
      setScorersLoading(true);
      const scorers = await scorersRef.current({
        ...scorerScopeParams,
        pageSize: TOP_SCORERS_COUNT,
        pageNumber: 1,
      });
      setTopScorers(scorers?.items ?? []);
      setScorersLoading(false);
    };

    void loadScorers();
  }, [scorerScopeParams]);

  const handleSeasonChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setSelectedSeasonId(event.target.value as GUID | '');
    // Reset the torneo: its options depend on the chosen temporada.
    setSelectedTournamentId('');
  };

  const handleTournamentChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    setSelectedTournamentId(event.target.value as GUID | '');
  };

  const handleClearFilters = () => {
    setSelectedSeasonId('');
    setSelectedTournamentId('');
  };

  if (loading || !summary) {
    return (
      <PageShell title="Estadísticas">
        <CardGridSkeleton count={4} />
      </PageShell>
    );
  }

  const matchesTotal = summary.matchesPlayed + summary.matchesScheduled;
  const playedPercent =
    matchesTotal === 0 ? 0 : (summary.matchesPlayed / matchesTotal) * 100;
  const maxScorerPoints = topScorers[0]?.points ?? 0;

  return (
    <PageShell title="Estadísticas">
      <FilterBar
        ariaLabel="Filtros de estadísticas"
        onClear={isScoped ? handleClearFilters : undefined}
      >
        <TextField
          select
          label="Temporada"
          size="small"
          value={selectedSeasonId}
          onChange={handleSeasonChange}
          sx={{ minWidth: 200 }}
        >
          <MenuItem value="">Todas</MenuItem>
          {(seasons ?? []).map(season => (
            <MenuItem key={season.id} value={season.id}>
              {season.name}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          select
          label="Torneo"
          size="small"
          value={selectedTournamentId}
          onChange={handleTournamentChange}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {tournamentOptions.map(option => (
            <MenuItem key={option.id} value={option.id}>
              {option.name}
            </MenuItem>
          ))}
        </TextField>
      </FilterBar>

      <Grid container spacing={2} sx={{
        mb: 1
      }}>
        <Grid
          size={{
            xs: 6,
            md: 3
          }}>
          <StatCard
            label="Torneos"
            value={summary.tournamentsTotal}
            color="#1976d2"
          />
        </Grid>
        <Grid
          size={{
            xs: 6,
            md: 3
          }}>
          <StatCard
            label="Equipos"
            value={summary.teamsTotal}
            color="#2e7d32"
          />
        </Grid>
        <Grid
          size={{
            xs: 6,
            md: 3
          }}>
          <StatCard
            label="Partidos jugados"
            value={summary.matchesPlayed}
            color="#ed6c02"
          />
        </Grid>
        <Grid
          size={{
            xs: 6,
            md: 3
          }}>
          <StatCard
            label="Sanciones"
            value={summary.sanctionsTotal}
            color="#d32f2f"
          />
        </Grid>
      </Grid>

      <Grid container spacing={2} sx={{
        mt: 1
      }}>
        <Grid
          size={{
            xs: 12,
            md: 4
          }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" sx={{
                mb: 2
              }}>
                Torneos por estado
              </Typography>
              <Stack spacing={1.5}>
                {Object.entries(summary.tournamentsByStatus).map(
                  ([status, count]) => (
                    <Box
                      key={status}
                      sx={{
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between"
                      }}>
                      <Box
                        sx={{
                          display: "flex",
                          alignItems: "center",
                          gap: 1
                        }}>
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
                      <Typography variant="body2" sx={{
                        fontWeight: 600
                      }}>
                        {count}
                      </Typography>
                    </Box>
                  )
                )}
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid
          size={{
            xs: 12,
            md: 4
          }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" sx={{
                mb: 2
              }}>
                Partidos
              </Typography>
              <Stack spacing={2}>
                <Box>
                  <Box
                    sx={{
                      display: "flex",
                      justifyContent: "space-between",
                      mb: 0.5
                    }}>
                    <Typography variant="body2">Jugados</Typography>
                    <Typography variant="body2" sx={{
                      fontWeight: 600
                    }}>
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
                  sx={{
                    display: "flex",
                    justifyContent: "space-between"
                  }}>
                  <Typography variant="body2" sx={{
                    color: "text.secondary"
                  }}>
                    Programados
                  </Typography>
                  <Typography variant="body2" sx={{
                    fontWeight: 600
                  }}>
                    {summary.matchesScheduled}
                  </Typography>
                </Box>
              </Stack>
            </CardContent>
          </Card>
        </Grid>

        <Grid
          size={{
            xs: 12,
            md: 4
          }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="h6" sx={{
                mb: 2
              }}>
                Top goleadores
              </Typography>
              {scorersLoading ? (
                <TableSkeleton rows={TOP_SCORERS_COUNT} columns={3} />
              ) : topScorers.length === 0 ? (
                <Typography variant="body2" sx={{
                  color: "text.secondary"
                }}>
                  {isScoped
                    ? 'No hay goleadores para el filtro seleccionado.'
                    : 'Sin datos de goleadores.'}
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
                    {topScorers.map((scorer, index) => (
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
                          <Typography variant="body2" sx={{
                            fontWeight: 600
                          }}>
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
    </PageShell>
  );
};

export default StatisticsPage;
