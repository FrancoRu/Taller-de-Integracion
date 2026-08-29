import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
import {
  Box,
  Chip,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  IPlayerSanctionResponse,
  PlayerSanctionsSearchFilters,
} from '@/modules/playerSanction/type/playerSanction.d';
import {
  formatFechasRemaining,
  formatSanctionDurationFechas,
  getSanctionStateLabel,
  getSanctionSubjectName,
  getSanctionSubjectTypeLabel,
} from '@/modules/playerSanction/utils/sanctionDisplay';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import PlayerSanctionCreatePage from '@/views/playerSanction/playerSanctionCreatePage';
import PlayerSanctionDeletePage from '@/views/playerSanction/playerSanctionDeletePage';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
  FILTER_OPTIONS_PAGE_SIZE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';

const EMPTY_FILTERS: PlayerSanctionsSearchFilters = {};

const formatDateTime = (value?: string | Date | null) => formatDateTimeAr(value);

const PlayerSanctionsPage: React.FC = () => {
  const navigate = useNavigate();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const { matches, getMatchByFilter } = useMatch();
  const { players, getPlayersByFilter } = usePlayer();
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] =
    useState<PlayerSanctionsSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<PlayerSanctionsSearchFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [deletingSanction, setDeletingSanction] =
    useState<IPlayerSanctionResponse | null>(null);
  const getPlayerSanctionByFilterRef = useRef(getPlayerSanctionByFilter);
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);
  const getDivisionsByFiltersRef = useRef(getDivisionsByFilters);
  const getStagesByFiltersRef = useRef(getStagesByFilters);
  const getMatchByFilterRef = useRef(getMatchByFilter);
  const getPlayersByFilterRef = useRef(getPlayersByFilter);

  useEffect(() => {
    getPlayerSanctionByFilterRef.current = getPlayerSanctionByFilter;
  }, [getPlayerSanctionByFilter]);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getDivisionsByFiltersRef.current = getDivisionsByFilters;
  }, [getDivisionsByFilters]);

  useEffect(() => {
    getStagesByFiltersRef.current = getStagesByFilters;
  }, [getStagesByFilters]);

  useEffect(() => {
    getMatchByFilterRef.current = getMatchByFilter;
  }, [getMatchByFilter]);

  useEffect(() => {
    getPlayersByFilterRef.current = getPlayersByFilter;
  }, [getPlayersByFilter]);

  useEffect(() => {
    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  useEffect(() => {
    if (!filters.tournamentId) {
      return;
    }

    void getDivisionsByFiltersRef.current({
      tournamentId: filters.tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.tournamentId]);

  useEffect(() => {
    if (!filters.divisionId) {
      return;
    }

    void getStagesByFiltersRef.current({
      divisionId: filters.divisionId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.divisionId]);

  useEffect(() => {
    if (!filters.stageId) {
      return;
    }

    void getMatchByFilterRef.current({
      stageId: filters.stageId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.stageId]);

  useEffect(() => {
    if (!filters.teamId) {
      return;
    }

    void getPlayersByFilterRef.current({
      teamId: filters.teamId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.teamId]);

  const fetchSanctions = useCallback(
    async (
      activeFilters: PlayerSanctionsSearchFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getPlayerSanctionByFilterRef.current({
        ...activeFilters,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchSanctions(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchSanctions, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as PlayerSanctionsSearchFilters;

    if (name === 'tournamentId') {
      updated.tournamentId = (value as GUID) || undefined;
      updated.divisionId = undefined;
      updated.stageId = undefined;
      updated.matchId = undefined;
      updated.teamId = undefined;
      updated.playerId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'divisionId') {
      updated.divisionId = (value as GUID) || undefined;
      updated.stageId = undefined;
      updated.matchId = undefined;
      updated.teamId = undefined;
      updated.playerId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'stageId') {
      updated.stageId = (value as GUID) || undefined;
      updated.matchId = undefined;
      updated.teamId = undefined;
      updated.playerId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'matchId') {
      updated.matchId = (value as GUID) || undefined;
      updated.teamId = undefined;
      updated.playerId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'teamId') {
      updated.teamId = (value as GUID) || undefined;
      updated.playerId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'playerId') {
      updated.playerId = (value as GUID) || undefined;
      setDebouncedFilters(updated);
    }

    setFilters(updated);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
    setDebouncedFilters(EMPTY_FILTERS);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

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

  const handleView = useCallback(
    (row: IPlayerSanctionResponse) => {
      navigate(APP_ROUTES.panelSanction.build(row.slug));
    },
    [navigate]
  );

  const handleEdit = useCallback(
    (row: IPlayerSanctionResponse) => {
      navigate(APP_ROUTES.panelSanctionEdit.build(row.id));
    },
    [navigate]
  );

  const handleDelete = useCallback((row: IPlayerSanctionResponse) => {
    setDeletingSanction(row);
  }, []);

  const sanctionActions = useMemo<TableRowAction<IPlayerSanctionResponse>[]>(
    () => [
      {
        label: 'Ver',
        color: 'info',
        icon: <VisibilityIcon fontSize="small" />,
        onClick: handleView,
      },
      {
        label: 'Editar',
        color: 'primary',
        icon: <EditIcon fontSize="small" />,
        onClick: handleEdit,
      },
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: handleDelete,
      },
    ],
    [handleDelete, handleEdit, handleView]
  );

  const handleCreateSanction = useCallback(() => {
    setIsCreateModalOpen(true);
  }, []);

  const handleCloseCreateModal = useCallback(() => {
    setIsCreateModalOpen(false);
  }, []);

  const handleCreatedSanction = useCallback(() => {
    void fetchSanctions(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchSanctions, paginationModel]);

  const handleDeletedSanction = useCallback(() => {
    setDeletingSanction(null);
    void fetchSanctions(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchSanctions, paginationModel]);

  const columns: GridColDef<IPlayerSanctionResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IPlayerSanctionResponse>[] = [
      {
        field: 'subject',
        headerName: 'Sujeto',
        flex: 1.2,
        minWidth: 200,
        sortable: false,
        renderCell: params => getSanctionSubjectName(params.row),
      },
      {
        field: 'subjectType',
        headerName: 'Tipo',
        flex: 0.6,
        minWidth: 100,
        renderCell: params => getSanctionSubjectTypeLabel(params.row),
      },
      {
        field: 'duration',
        headerName: 'Duración (fechas)',
        flex: 0.7,
        minWidth: 130,
        renderCell: params => formatSanctionDurationFechas(params.row.duration),
      },
      {
        field: 'fechasRemaining',
        headerName: 'Fechas restantes',
        flex: 0.7,
        minWidth: 130,
        renderCell: params => formatFechasRemaining(params.row.fechasRemaining),
      },
      {
        field: 'isActive',
        headerName: 'Estado',
        flex: 0.6,
        minWidth: 110,
        renderCell: params => (
          <Chip
            size="small"
            label={getSanctionStateLabel(params.row)}
            color={params.row.isActive ? 'warning' : 'default'}
          />
        ),
      },
      {
        field: 'issuedDate',
        headerName: 'Fecha',
        flex: 0.9,
        minWidth: 150,
        renderCell: params => formatDateTime(params.row.issuedDate),
      },
      {
        field: 'description',
        headerName: 'Descripción',
        flex: 1.4,
        minWidth: 240,
      },
    ];

    return [...baseColumns, buildActionsColumn(sanctionActions)];
  }, [sanctionActions]);

  const rows = useMemo(() => playerSanctions ?? [], [playerSanctions]);

  const hasActiveFilters = useMemo(
    () =>
      Boolean(filters.tournamentId) ||
      Boolean(filters.divisionId) ||
      Boolean(filters.stageId) ||
      Boolean(filters.matchId) ||
      Boolean(filters.teamId) ||
      Boolean(filters.playerId) ||
      Boolean(filters.description),
    [
      filters.description,
      filters.divisionId,
      filters.matchId,
      filters.playerId,
      filters.stageId,
      filters.teamId,
      filters.tournamentId,
    ]
  );

  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron sanciones para el filtro aplicado.'
    : 'No hay sanciones cargadas.';

  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const divisionOptions = useMemo(() => divisions ?? [], [divisions]);
  const stageOptions = useMemo(() => stages ?? [], [stages]);
  const matchOptions = useMemo(() => matches ?? [], [matches]);
  const selectedMatch = useMemo(
    () => matchOptions.find(matchOption => matchOption.id === filters.matchId),
    [filters.matchId, matchOptions]
  );
  const teamOptions = useMemo(() => {
    const homeTeam = selectedMatch?.homeTeam;
    const visitorTeam = selectedMatch?.visitorTeam;

    return [homeTeam, visitorTeam].filter(
      (team): team is NonNullable<typeof homeTeam> => Boolean(team)
    );
  }, [selectedMatch?.homeTeam, selectedMatch?.visitorTeam]);
  const playerOptions = useMemo(
    () => (filters.teamId ? (players ?? []) : []),
    [filters.teamId, players]
  );

  return (
    <PageShell
      title="Sanciones"
      actions={<NewEntityButton type="Sanción" onClick={handleCreateSanction} />}
    >
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de sanciones">
        <Stack spacing={2} sx={{ width: '100%' }}>
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              alignItems: 'start',
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'repeat(2, minmax(0, 1fr))',
                md: 'repeat(3, minmax(0, 1fr))',
                lg: '0.9fr 0.9fr 1.1fr 1.5fr 0.9fr 1.1fr',
              },
            }}
          >
            <TextField
              select
              label="Torneo"
              name="tournamentId"
              size="small"
              value={filters.tournamentId ?? ''}
              onChange={handleFilterChange}
              fullWidth
            >
              <MenuItem value="">Todos</MenuItem>
              {tournamentOptions.map(tournamentOption => (
                <MenuItem key={tournamentOption.id} value={tournamentOption.id}>
                  {tournamentOption.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="División"
              name="divisionId"
              size="small"
              value={filters.divisionId ?? ''}
              onChange={handleFilterChange}
              disabled={!filters.tournamentId}
              fullWidth
            >
              <MenuItem value="">Todas</MenuItem>
              {divisionOptions.map(divisionOption => (
                <MenuItem key={divisionOption.id} value={divisionOption.id}>
                  {divisionOption.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Fase"
              name="stageId"
              size="small"
              value={filters.stageId ?? ''}
              onChange={handleFilterChange}
              disabled={!filters.divisionId}
              fullWidth
            >
              <MenuItem value="">Todas</MenuItem>
              {stageOptions.map(stageOption => (
                <MenuItem key={stageOption.id} value={stageOption.id}>
                  {stageOption.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Partido"
              name="matchId"
              size="small"
              value={filters.matchId ?? ''}
              onChange={handleFilterChange}
              disabled={!filters.stageId}
              fullWidth
            >
              <MenuItem value="">Todos</MenuItem>
              {matchOptions.map(matchOption => (
                <MenuItem key={matchOption.id} value={matchOption.id}>
                  {`${formatDateTime(matchOption.matchDate)} - ${matchOption.homeTeam?.name ?? '—'} vs ${matchOption.visitorTeam?.name ?? '—'}`}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Equipo"
              name="teamId"
              size="small"
              value={filters.teamId ?? ''}
              onChange={handleFilterChange}
              disabled={!filters.matchId}
              fullWidth
            >
              <MenuItem value="">Todos</MenuItem>
              {teamOptions.map(teamOption => (
                <MenuItem key={teamOption.id} value={teamOption.id}>
                  {teamOption.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Jugador"
              name="playerId"
              size="small"
              value={filters.playerId ?? ''}
              onChange={handleFilterChange}
              disabled={!filters.teamId}
              fullWidth
            >
              <MenuItem value="">Todos</MenuItem>
              {playerOptions.map(playerOption => (
                <MenuItem key={playerOption.id} value={playerOption.id}>
                  {playerOption.fullName}
                </MenuItem>
              ))}
            </TextField>
          </Box>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              placeholder="Descripción"
              name="description"
              size="small"
              value={filters.description ?? ''}
              onChange={handleFilterChange}
              sx={{ minWidth: 240, width: { xs: '100%', sm: 320 } }}
              slotProps={{
                input: {
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon fontSize="small" />
                    </InputAdornment>
                  ),
                },

                htmlInput: { 'aria-label': 'Descripción' }
              }} />
          </Stack>
        </Stack>
      </FilterBar>

      <Box sx={{ width: '100%' }}>
        <DataGrid
            rows={rows}
            columns={columns}
            loading={loading}
            getRowId={row => row.id}
            autoHeight
            disableRowSelectionOnClick
            disableColumnMenu
            localeText={{ noRowsLabel: noRowsMessage }}
            pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
            paginationModel={paginationModel}
            onPaginationModelChange={handlePaginationModelChange}
            paginationMode="server"
            rowCount={rowCount}
          />
        </Box>

        <PlayerSanctionCreatePage
          open={isCreateModalOpen}
          onClose={handleCloseCreateModal}
          onCreated={handleCreatedSanction}
        />

        <PlayerSanctionDeletePage
          open={Boolean(deletingSanction)}
          sanction={deletingSanction}
          onClose={() => setDeletingSanction(null)}
          onDeleted={handleDeletedSanction}
        />
    </PageShell>
  );
};

export default PlayerSanctionsPage;
