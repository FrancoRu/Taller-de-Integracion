import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { MenuItem, Stack, TextField } from '@mui/material';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse } from '@/modules/match/type/match';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const formatMatchLabel = (match: IMatchResponse) => {
  const homeTeamName = match.homeTeam?.name ?? 'Equipo local';
  const visitorTeamName = match.visitorTeam?.name ?? 'Equipo visitante';
  const matchDate = new Date(match.matchDate);
  const formattedDate = Number.isNaN(matchDate.getTime())
    ? ''
    : ` · ${matchDate.toLocaleDateString('es-AR')}`;

  return `${homeTeamName} vs ${visitorTeamName}${formattedDate}`;
};

const PlayerScorersTab: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { getMatchByFilter } = useMatch();
  const { getTeamsByFiltered } = useTeam();
  const { scorersByPlayer, getScorersByPlayerFiltered } = useScorer();
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>(
    ''
  );
  const [selectedMatchId, setSelectedMatchId] = useState<GUID | ''>('');
  const [selectedTeamId, setSelectedTeamId] = useState<GUID | ''>('');
  const [matchOptions, setMatchOptions] = useState<IMatchResponse[]>([]);
  const [teamOptions, setTeamOptions] = useState<ITeamResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);
  const getMatchByFilterRef = useRef(getMatchByFilter);
  const getTeamsByFilteredRef = useRef(getTeamsByFiltered);
  const getScorersByPlayerFilteredRef = useRef(getScorersByPlayerFiltered);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getMatchByFilterRef.current = getMatchByFilter;
  }, [getMatchByFilter]);

  useEffect(() => {
    getTeamsByFilteredRef.current = getTeamsByFiltered;
  }, [getTeamsByFiltered]);

  useEffect(() => {
    getScorersByPlayerFilteredRef.current = getScorersByPlayerFiltered;
  }, [getScorersByPlayerFiltered]);

  useEffect(() => {
    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  useEffect(() => {
    if (!selectedTournamentId) {
      setMatchOptions([]);
      setTeamOptions([]);
      return;
    }

    const loadPlayerFilterOptions = async () => {
      const [matchesResponse, teamsResponse] = await Promise.all([
        getMatchByFilterRef.current({
          tournamentId: selectedTournamentId,
          pageSize: FILTER_OPTIONS_PAGE_SIZE,
        }),
        getTeamsByFilteredRef.current({
          tournamentId: selectedTournamentId,
          pageSize: FILTER_OPTIONS_PAGE_SIZE,
        }),
      ]);

      setMatchOptions(matchesResponse?.items ?? []);
      setTeamOptions(teamsResponse?.items ?? []);
    };

    void loadPlayerFilterOptions();
  }, [selectedTournamentId]);

  const fetchScorers = useCallback(
    async (
      activePaginationModel: GridPaginationModel,
      activeTournamentId: GUID | '',
      activeMatchId: GUID | '',
      activeTeamId: GUID | ''
    ) => {
      setLoading(true);

      const response = await getScorersByPlayerFilteredRef.current({
        tournamentId: activeTournamentId || undefined,
        matchId: activeMatchId || undefined,
        teamId: activeTeamId || undefined,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });

      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchScorers(
      paginationModel,
      selectedTournamentId,
      selectedMatchId,
      selectedTeamId
    );
  }, [
    fetchScorers,
    paginationModel,
    selectedMatchId,
    selectedTeamId,
    selectedTournamentId,
  ]);

  const resetToFirstPage = useCallback(() => {
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  }, []);

  const handleTournamentChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedTournamentId(event.target.value as GUID | '');
      setSelectedMatchId('');
      setSelectedTeamId('');
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handleMatchChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedMatchId(event.target.value as GUID | '');
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handleTeamChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedTeamId(event.target.value as GUID | '');
      resetToFirstPage();
    },
    [resetToFirstPage]
  );

  const handlePaginationModelChange = useCallback(
    (nextPaginationModel: GridPaginationModel) => {
      setPaginationModel(prev =>
        prev.page === nextPaginationModel.page &&
        prev.pageSize === nextPaginationModel.pageSize
          ? prev
          : nextPaginationModel
      );
    },
    []
  );

  const columns = useMemo<GridColDef<IScorerByPlayerResponse>[]>(
    () => [
      {
        field: 'fullName',
        headerName: 'Jugador',
        flex: 1.4,
        minWidth: 220,
      },
      {
        field: 'points',
        headerName: 'Puntos',
        flex: 0.8,
        minWidth: 120,
      },
    ],
    []
  );

  return (
    <>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
        <TextField
          select
          label="Torneo"
          size="small"
          value={selectedTournamentId}
          onChange={handleTournamentChange}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {(tournaments ?? []).map(tournament => (
            <MenuItem key={tournament.id} value={tournament.id}>
              {tournament.name}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          select
          label="Partido"
          size="small"
          value={selectedMatchId}
          onChange={handleMatchChange}
          sx={{ minWidth: 280 }}
          disabled={!selectedTournamentId}
        >
          <MenuItem value="">Todos</MenuItem>
          {matchOptions.map(match => (
            <MenuItem key={match.id} value={match.id}>
              {formatMatchLabel(match)}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          select
          label="Equipo"
          size="small"
          value={selectedTeamId}
          onChange={handleTeamChange}
          sx={{ minWidth: 220 }}
          disabled={!selectedTournamentId}
        >
          <MenuItem value="">Todos</MenuItem>
          {teamOptions.map(team => (
            <MenuItem key={team.id} value={team.id}>
              {team.name}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <DataGrid
        rows={scorersByPlayer ?? []}
        columns={columns}
        getRowId={row => row.playerId}
        loading={loading}
        autoHeight
        disableRowSelectionOnClick
        disableColumnMenu
        paginationMode="server"
        rowCount={rowCount}
        pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        localeText={{
          noRowsLabel: 'No hay puntuaciones registradas por jugador.',
        }}
      />
    </>
  );
};

export default PlayerScorersTab;
