import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Box,
  CircularProgress,
  Container,
  MenuItem,
  Pagination,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import MatchCard from './MatchCard';

const PAGE_SIZE = 12;

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
