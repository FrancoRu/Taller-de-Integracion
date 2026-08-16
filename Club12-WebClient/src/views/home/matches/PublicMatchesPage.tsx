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
import DivisionStagePicker from '@/views/core/components/DivisionStagePicker';
import MatchFixtureList from './MatchFixtureList';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const PAGE_SIZE = 12;

export default function PublicMatchesPage() {
  const { matches, getMatchByFilter } = useMatch();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>('');
  const [selectedDivisionId, setSelectedDivisionId] = useState<GUID | ''>('');
  const [selectedStageId, setSelectedStageId] = useState<GUID | ''>('');
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
    void getAllTournamentsRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  const fetchMatches = useCallback(
    async (
      tournamentId: GUID | '',
      divisionId: GUID | '',
      stageId: GUID | '',
      finished: '' | 'true' | 'false',
      currentPage: number
    ) => {
      setLoading(true);
      const response = await getMatchByFilterRef.current({
        tournamentId: tournamentId || undefined,
        divisionId: divisionId || undefined,
        stageId: stageId || undefined,
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
    void fetchMatches(selectedTournamentId, selectedDivisionId, selectedStageId, isFinished, page);
  }, [fetchMatches, selectedTournamentId, selectedDivisionId, selectedStageId, isFinished, page]);

  const handleTournamentChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setSelectedTournamentId(e.target.value as GUID | '');
      setSelectedDivisionId('');
      setSelectedStageId('');
      setPage(1);
    },
    []
  );

  const handleDivisionChange = useCallback((divisionId: GUID | '') => {
    setSelectedDivisionId(divisionId);
    setPage(1);
  }, []);

  const handleStageChange = useCallback((stageId: GUID | '') => {
    setSelectedStageId(stageId);
    setPage(1);
  }, []);

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
      <Typography variant="h4" component="h1" fontWeight="bold" mb={1}>
        Partidos
      </Typography>
      <Typography variant="body1" color="text.secondary" mb={3}>
        Fixture y resultados de la liga.
      </Typography>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={4} flexWrap="wrap">
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

        <DivisionStagePicker
          tournamentId={selectedTournamentId}
          divisionId={selectedDivisionId}
          stageId={selectedStageId}
          onDivisionChange={handleDivisionChange}
          onStageChange={handleStageChange}
        />

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
          <MatchFixtureList matches={rows} />

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
