import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Card,
  CardContent,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse } from '@/modules/match/type/match';
import { useScorer } from '@/modules/scorer/hook/scorer.hook';
import {
  IScorerByPlayerResponse,
  IScorerByTeamResponse,
  ScorersViewMode,
} from '@/modules/scorer/type/scorer.d';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';

const formatMatchLabel = (match: IMatchResponse) => {
  const homeTeamName = match.homeTeam?.name ?? 'Equipo local';
  const visitorTeamName = match.visitorTeam?.name ?? 'Equipo visitante';
  const matchDate = new Date(match.matchDate);
  const formattedDate = Number.isNaN(matchDate.getTime())
    ? ''
    : ` · ${matchDate.toLocaleDateString('es-AR')}`;

  return `${homeTeamName} vs ${visitorTeamName}${formattedDate}`;
};

const PlayerStatisticsPage: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { getMatchByFilter } = useMatch();
  const { getTeamsByFiltered } = useTeam();
  const {
    scorersByPlayer,
    scorersByTeam,
    getScorersByPlayerFiltered,
    getScorersByTeamFiltered,
  } = useScorer();
  const [tab, setTab] = useState<ScorersViewMode>('team');
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
  const getScorersByTeamFilteredRef = useRef(getScorersByTeamFiltered);
  const getScorersByPlayerFilteredRef = useRef(getScorersByPlayerFiltered);
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);
  const getMatchByFilterRef = useRef(getMatchByFilter);
  const getTeamsByFilteredRef = useRef(getTeamsByFiltered);

  useEffect(() => {
    getScorersByTeamFilteredRef.current = getScorersByTeamFiltered;
  }, [getScorersByTeamFiltered]);

  useEffect(() => {
    getScorersByPlayerFilteredRef.current = getScorersByPlayerFiltered;
  }, [getScorersByPlayerFiltered]);

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
    void getAllTournamentsByFilterRef.current({ pageSize: 300 });
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
          pageSize: 300,
        }),
        getTeamsByFilteredRef.current({
          tournamentId: selectedTournamentId,
          pageSize: 300,
        }),
      ]);

      setMatchOptions(matchesResponse?.items ?? []);
      setTeamOptions(teamsResponse?.items ?? []);
    };

    void loadPlayerFilterOptions();
  }, [selectedTournamentId]);

  const fetchScorers = useCallback(
    async (
      activeTab: ScorersViewMode,
      activePaginationModel: GridPaginationModel,
      activeTournamentId: GUID | '',
      activeMatchId: GUID | '',
      activeTeamId: GUID | ''
    ) => {
      setLoading(true);

      const tournamentId = activeTournamentId || undefined;
      const response =
        activeTab === 'team'
          ? await getScorersByTeamFilteredRef.current({
              tournamentId,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            })
          : await getScorersByPlayerFilteredRef.current({
              tournamentId,
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
      tab,
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
    tab,
  ]);

  const handleTabChange = useCallback(
    (_: React.SyntheticEvent, value: ScorersViewMode) => {
      setTab(value);
      setPaginationModel(prev =>
        prev.page === 0 ? prev : { ...prev, page: 0 }
      );
    },
    []
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

  const handleTournamentChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedTournamentId(event.target.value as GUID | '');
      setSelectedMatchId('');
      setSelectedTeamId('');
      setPaginationModel(prev =>
        prev.page === 0 ? prev : { ...prev, page: 0 }
      );
    },
    []
  );

  const handleMatchChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedMatchId(event.target.value as GUID | '');
      setPaginationModel(prev =>
        prev.page === 0 ? prev : { ...prev, page: 0 }
      );
    },
    []
  );

  const handleTeamChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
      setSelectedTeamId(event.target.value as GUID | '');
      setPaginationModel(prev =>
        prev.page === 0 ? prev : { ...prev, page: 0 }
      );
    },
    []
  );

  const teamColumns = useMemo<GridColDef<IScorerByTeamResponse>[]>(
    () => [
      {
        field: 'name',
        headerName: 'Equipo',
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

  const playerColumns = useMemo<GridColDef<IScorerByPlayerResponse>[]>(
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
    <Card>
      <CardContent>
        <Typography variant="h6" mb={2}>
          Puntuaciones
        </Typography>

        <Tabs
          value={tab}
          onChange={handleTabChange}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Por equipo" value="team" />
          <Tab label="Por jugador" value="player" />
        </Tabs>

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

          {tab === 'player' && (
            <>
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
            </>
          )}
        </Stack>

        {tab === 'team' ? (
          <DataGrid
            rows={scorersByTeam ?? []}
            columns={teamColumns}
            getRowId={row => row.teamId}
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
              noRowsLabel: 'No hay puntuaciones registradas por equipo.',
            }}
          />
        ) : (
          <DataGrid
            rows={scorersByPlayer ?? []}
            columns={playerColumns}
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
        )}
      </CardContent>
    </Card>
  );
};

export default PlayerStatisticsPage;
