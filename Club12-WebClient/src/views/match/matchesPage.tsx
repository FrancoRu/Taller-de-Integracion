import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Box,
  Card,
  CardContent,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  confirmDelete,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse, MatchFiltered } from '@/modules/match/type/match';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import TeamLogo from '@/views/core/components/TeamLogo';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
  FILTER_OPTIONS_PAGE_SIZE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';

interface MatchesPageProps {
  stageId?: GUID;
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

type MatchesSearchFilters = Pick<
  MatchFiltered,
  | 'homeTeamName'
  | 'visitorTeamName'
  | 'tournamentId'
  | 'divisionId'
  | 'stageId'
  | 'isFinished'
  | 'type'
>;

const EMPTY_FILTERS: MatchesSearchFilters = {};

const formatDateTime = (value?: string | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleString('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short',
  });
};

const MatchesPage: React.FC<MatchesPageProps> = ({
  stageId,
  emptyMessage = 'No hay partidos cargados.',
  title = 'Partidos',
  wrapInCard = false,
  createType = 'Partido',
  onCreate,
}) => {
  const navigate = useNavigate();
  const { matches, getMatchByFilter, deleteMatchById } = useMatch();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<MatchesSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<MatchesSearchFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getMatchByFilterRef = useRef(getMatchByFilter);
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);
  const getDivisionsByFiltersRef = useRef(getDivisionsByFilters);
  const getStagesByFiltersRef = useRef(getStagesByFilters);

  useEffect(() => {
    getMatchByFilterRef.current = getMatchByFilter;
  }, [getMatchByFilter]);

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
    if (stageId) {
      return;
    }

    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [stageId]);

  useEffect(() => {
    if (stageId || !filters.tournamentId) {
      return;
    }

    void getDivisionsByFiltersRef.current({
      tournamentId: filters.tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.tournamentId, stageId]);

  useEffect(() => {
    if (stageId || !filters.divisionId) {
      return;
    }

    void getStagesByFiltersRef.current({
      divisionId: filters.divisionId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [filters.divisionId, stageId]);

  const fetchMatches = useCallback(
    async (
      activeFilters: MatchesSearchFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getMatchByFilterRef.current(
        stageId
          ? {
              stageId,
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
          : {
              tournamentId: activeFilters.tournamentId,
              divisionId: activeFilters.divisionId,
              stageId: activeFilters.stageId,
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
      );
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    [stageId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchMatches(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchMatches, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as MatchesSearchFilters;

    if (name === 'tournamentId') {
      updated.tournamentId = (value as GUID) || undefined;
      updated.divisionId = undefined;
      updated.stageId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'divisionId') {
      updated.divisionId = (value as GUID) || undefined;
      updated.stageId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'stageId') {
      updated.stageId = (value as GUID) || undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'isFinished') {
      updated.isFinished =
        value === '' ? undefined : value.toLowerCase() === 'true';
      setDebouncedFilters(updated);
    }

    if (name === 'type') {
      updated.type = (value as MatchFiltered['type']) || undefined;
      setDebouncedFilters(updated);
    }

    setFilters(updated);
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
    (row: IMatchResponse) => {
      navigate(APP_ROUTES.panelMatch.build(row.id));
    },
    [navigate]
  );

  /** No-op until the panel route for editing a match by id is implemented. */
  const handleEdit = useCallback((_row: IMatchResponse) => {}, []);

  const handleDelete = useCallback(
    async (row: IMatchResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar este partido?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteMatchById(row.id);
      await notifySuccess({
        title: '¡Eliminado!',
        text: 'El partido ha sido eliminado.',
      });
    },
    [deleteMatchById]
  );

  const matchActions = useMemo<TableRowAction<IMatchResponse>[]>(
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
        disabled: true,
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

  const columns: GridColDef<IMatchResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IMatchResponse>[] = [
      {
        field: 'matchDate',
        headerName: 'Fecha',
        flex: 1,
        minWidth: 170,
        renderCell: params => formatDateTime(params.row.matchDate),
      },
      {
        field: 'homeTeam',
        headerName: 'Local',
        flex: 1.1,
        minWidth: 190,
        sortable: false,
        renderCell: params => {
          const team = params.row.homeTeam;

          if (!team) {
            return '—';
          }

          return (
            <Stack direction="row" spacing={1} sx={{
              alignItems: "center"
            }}>
              <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={28} />
              <Typography variant="body2">{team.name}</Typography>
            </Stack>
          );
        },
      },
      {
        field: 'visitorTeam',
        headerName: 'Visitante',
        flex: 1.1,
        minWidth: 190,
        sortable: false,
        renderCell: params => {
          const team = params.row.visitorTeam;

          if (!team) {
            return '—';
          }

          return (
            <Stack direction="row" spacing={1} sx={{
              alignItems: "center"
            }}>
              <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={28} />
              <Typography variant="body2">{team.name}</Typography>
            </Stack>
          );
        },
      },
      {
        field: 'result',
        headerName: 'Resultado',
        flex: 0.8,
        minWidth: 120,
        sortable: false,
        renderCell: params => {
          const homeScore = params.row.homeTeam?.score;
          const visitorScore = params.row.visitorTeam?.score;

          if (homeScore == null || visitorScore == null) {
            return '—';
          }

          return `${homeScore} - ${visitorScore}`;
        },
      },
      {
        field: 'matchType',
        headerName: 'Tipo',
        flex: 0.8,
        minWidth: 120,
      },
      {
        field: 'venue',
        headerName: 'Cancha',
        flex: 1,
        minWidth: 150,
        sortable: false,
        renderCell: params => params.row.venue?.name || '—',
      },
      {
        field: 'isFinished',
        headerName: 'Estado',
        flex: 0.8,
        minWidth: 110,
        renderCell: params =>
          params.row.isFinished ? 'Finalizado' : 'Programado',
      },
    ];

    return [...baseColumns, buildActionsColumn(matchActions)];
  }, [matchActions]);

  const rows = useMemo(() => matches ?? [], [matches]);
  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const divisionOptions = useMemo(
    () =>
      (divisions ?? []).filter(divisionOption =>
        filters.tournamentId
          ? divisionOption.tournamentId === filters.tournamentId
          : true
      ),
    [divisions, filters.tournamentId]
  );
  const stageOptions = useMemo(
    () =>
      (stages ?? []).filter(stageOption =>
        filters.divisionId
          ? stageOption.divisionId === filters.divisionId
          : true
      ),
    [filters.divisionId, stages]
  );
  const hasActiveFilters = useMemo(
    () =>
      Boolean(filters.homeTeamName) ||
      Boolean(filters.visitorTeamName) ||
      Boolean(filters.tournamentId) ||
      Boolean(filters.divisionId) ||
      Boolean(filters.stageId) ||
      Boolean(filters.type) ||
      filters.isFinished !== undefined,
    [
      filters.homeTeamName,
      filters.visitorTeamName,
      filters.tournamentId,
      filters.divisionId,
      filters.stageId,
      filters.type,
      filters.isFinished,
    ]
  );
  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron partidos para el filtro aplicado.'
    : emptyMessage;

  const handleCreateMatch = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    navigate(
      stageId
        ? `${APP_ROUTES.panelMatchCreate}?stageId=${stageId}`
        : APP_ROUTES.panelMatchCreate
    );
  }, [onCreate, navigate, stageId]);

  const content = (
    <>
      {(title || createType) && (
        <Stack
          direction="row"
          sx={{
            justifyContent: "space-between",
            alignItems: "center",
            mb: 2
          }}>
          {title ? <Typography variant="h6">{title}</Typography> : <Box />}
          <NewEntityButton type={createType} onClick={handleCreateMatch} />
        </Stack>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{
        mb: 2
      }}>
        {!stageId && (
          <TextField
            select
            label="Tipo"
            name="type"
            size="small"
            value={filters.type ?? ''}
            onChange={handleFilterChange}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Todos</MenuItem>
            {Object.values(MatchType).map(matchType => (
              <MenuItem key={matchType} value={matchType}>
                {matchType}
              </MenuItem>
            ))}
          </TextField>
        )}

        {!stageId && (
          <TextField
            select
            label="Estado"
            name="isFinished"
            size="small"
            value={
              filters.isFinished === undefined
                ? ''
                : filters.isFinished
                  ? 'true'
                  : 'false'
            }
            onChange={handleFilterChange}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="false">Programado</MenuItem>
            <MenuItem value="true">Finalizado</MenuItem>
          </TextField>
        )}

        {!stageId && (
          <TextField
            select
            label="Torneo"
            name="tournamentId"
            size="small"
            value={filters.tournamentId ?? ''}
            onChange={handleFilterChange}
            sx={{ minWidth: 220 }}
          >
            <MenuItem value="">Todos</MenuItem>
            {tournamentOptions.map(tournamentOption => (
              <MenuItem key={tournamentOption.id} value={tournamentOption.id}>
                {tournamentOption.name}
              </MenuItem>
            ))}
          </TextField>
        )}

        {!stageId && (
          <TextField
            select
            label="División"
            name="divisionId"
            size="small"
            value={filters.divisionId ?? ''}
            onChange={handleFilterChange}
            sx={{ minWidth: 220 }}
            disabled={!filters.tournamentId}
          >
            <MenuItem value="">Todas</MenuItem>
            {divisionOptions.map(divisionOption => (
              <MenuItem key={divisionOption.id} value={divisionOption.id}>
                {divisionOption.name}
              </MenuItem>
            ))}
          </TextField>
        )}

        {!stageId && (
          <TextField
            select
            label="Fase"
            name="stageId"
            size="small"
            value={filters.stageId ?? ''}
            onChange={handleFilterChange}
            sx={{ minWidth: 220 }}
            disabled={!filters.divisionId}
          >
            <MenuItem value="">Todas</MenuItem>
            {stageOptions.map(stageOption => (
              <MenuItem key={stageOption.id} value={stageOption.id}>
                {stageOption.name}
              </MenuItem>
            ))}
          </TextField>
        )}

        <TextField
          label="Equipo local"
          name="homeTeamName"
          size="small"
          value={filters.homeTeamName ?? ''}
          onChange={handleFilterChange}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }
          }}
        />
        <TextField
          label="Equipo visitante"
          name="visitorTeamName"
          size="small"
          value={filters.visitorTeamName ?? ''}
          onChange={handleFilterChange}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }
          }}
        />
      </Stack>

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
    </>
  );

  if (wrapInCard) {
    return (
      <Card>
        <CardContent>{content}</CardContent>
      </Card>
    );
  }

  return <Box sx={{ width: '100%' }}>{content}</Box>;
};

export default MatchesPage;
