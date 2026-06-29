import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Divider,
  MenuItem,
  Pagination,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import TeamLogo from '@/views/core/components/TeamLogo';

const PAGE_SIZE = 12;

const formatDate = (value?: string | null) => {
  if (!value) return '—';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleString('es-AR', { dateStyle: 'medium', timeStyle: 'short' });
};

function MatchCard({ match }: { match: IMatchResponse }) {
  const home = match.homeTeam;
  const visitor = match.visitorTeam;
  const finished = match.isFinished;

  return (
    <Card variant="outlined">
      <CardContent sx={{ pb: '12px !important' }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1.5}>
          <Typography variant="caption" color="text.secondary">
            {formatDate(match.matchDate)}
          </Typography>
          <Chip
            label={finished ? 'Finalizado' : 'Programado'}
            size="small"
            color={finished ? 'success' : 'default'}
            variant="outlined"
          />
        </Stack>

        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
          <Stack alignItems="center" spacing={0.5} sx={{ flex: 1 }}>
            <TeamLogo teamName={home?.name ?? '?'} logoUrl={home?.logoUrl} size={40} />
            <Typography variant="body2" textAlign="center" fontWeight={500} lineHeight={1.2}>
              {home?.name ?? '—'}
            </Typography>
          </Stack>

          <Box textAlign="center" sx={{ minWidth: 64 }}>
            {finished ? (
              <Typography variant="h5" fontWeight="bold">
                {home?.score ?? 0} – {visitor?.score ?? 0}
              </Typography>
            ) : (
              <Typography variant="h6" color="text.secondary">
                vs
              </Typography>
            )}
          </Box>

          <Stack alignItems="center" spacing={0.5} sx={{ flex: 1 }}>
            <TeamLogo teamName={visitor?.name ?? '?'} logoUrl={visitor?.logoUrl} size={40} />
            <Typography variant="body2" textAlign="center" fontWeight={500} lineHeight={1.2}>
              {visitor?.name ?? '—'}
            </Typography>
          </Stack>
        </Stack>

        {match.venue && (
          <>
            <Divider sx={{ my: 1 }} />
            <Typography variant="caption" color="text.secondary">
              {match.venue.name}
            </Typography>
          </>
        )}
      </CardContent>
    </Card>
  );
}

export default function PublicMatchesPage() {
  const { matches, getMatchByFilter } = useMatch();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>('');
  const [isFinished, setIsFinished] = useState<'' | 'true' | 'false'>('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);

  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);
  const getMatchByFilterRef = useRef(getMatchByFilter);

  useEffect(() => {
    getAllTournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getMatchByFilterRef.current = getMatchByFilter;
  }, [getMatchByFilter]);

  useEffect(() => {
    void getAllTournamentsRef.current({ pageSize: 300 });
  }, []);

  const fetchMatches = useCallback(
    async (tournamentId: GUID | '', finished: '' | 'true' | 'false', currentPage: number) => {
      setLoading(true);
      const response = await getMatchByFilterRef.current({
        tournamentId: tournamentId || undefined,
        isFinished: finished === '' ? undefined : finished === 'true',
        pageNumber: currentPage,
        pageSize: PAGE_SIZE,
      });
      setTotalCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchMatches(selectedTournamentId, isFinished, page);
  }, [fetchMatches, selectedTournamentId, isFinished, page]);

  const handleTournamentChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setSelectedTournamentId(e.target.value as GUID | '');
      setPage(1);
    },
    []
  );

  const handleStatusChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setIsFinished(e.target.value as '' | 'true' | 'false');
      setPage(1);
    },
    []
  );

  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const rows = useMemo(() => matches ?? [], [matches]);
  const pageCount = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography variant="h4" fontWeight="bold" mb={1}>
        Partidos
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={3}>
        Fixture y resultados de la liga.
      </Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={4}>
        <TextField
          select
          label="Torneo"
          size="small"
          value={selectedTournamentId}
          onChange={handleTournamentChange}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {tournamentOptions.map(t => (
            <MenuItem key={t.id} value={t.id}>
              {t.name}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          select
          label="Estado"
          size="small"
          value={isFinished}
          onChange={handleStatusChange}
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">Todos</MenuItem>
          <MenuItem value="false">Programados</MenuItem>
          <MenuItem value="true">Finalizados</MenuItem>
        </TextField>
      </Stack>

      {loading ? (
        <Box display="flex" justifyContent="center" py={8}>
          <CircularProgress />
        </Box>
      ) : rows.length === 0 ? (
        <Typography color="text.secondary">No hay partidos disponibles.</Typography>
      ) : (
        <>
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'repeat(2, 1fr)',
                md: 'repeat(3, 1fr)',
              },
            }}
          >
            {rows.map(match => (
              <MatchCard key={match.id} match={match} />
            ))}
          </Box>

          {pageCount > 1 && (
            <Box display="flex" justifyContent="center" mt={4}>
              <Pagination
                count={pageCount}
                page={page}
                onChange={(_, value) => setPage(value)}
                color="primary"
              />
            </Box>
          )}
        </>
      )}
    </Container>
  );
}
