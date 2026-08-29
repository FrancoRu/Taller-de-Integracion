import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Box,
  Button,
  InputAdornment,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import { confirmDelete, notifySuccess } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  IStageListFilters,
  IStageResponse,
} from '@/modules/stage/type/stage';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import {
  DeleteIcon,
  SearchIcon,
  SettingsSuggestIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
  FILTER_OPTIONS_PAGE_SIZE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';
import { translateStageType } from '@/modules/core/utils/translateStageType';

interface StagesPageProps {
  divisionId?: GUID;
  showGenerateStagesButton?: boolean;
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

const EMPTY_FILTERS: IStageListFilters = {};

const formatDate = (value?: string | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleDateString('es-AR');
};


const StagesPage: React.FC<StagesPageProps> = ({
  divisionId,
  showGenerateStagesButton = false,
  emptyMessage = 'No hay fases cargadas.',
  title = 'Fases',
  wrapInCard = false,
  createType = 'Fase',
  onCreate,
}) => {
  const navigate = useNavigate();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const {
    stages,
    getStagesByFilters,
    deleteStagesById,
    generateStagesAutomatically,
  } = useStage();
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<IStageListFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<IStageListFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getStagesByFiltersRef = useRef(getStagesByFilters);
  const getDivisionsByFiltersRef = useRef(getDivisionsByFilters);
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);

  useEffect(() => {
    getStagesByFiltersRef.current = getStagesByFilters;
  }, [getStagesByFilters]);

  useEffect(() => {
    getDivisionsByFiltersRef.current = getDivisionsByFilters;
  }, [getDivisionsByFilters]);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    if (divisionId) {
      return;
    }

    void getAllTournamentsByFilterRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [divisionId]);

  useEffect(() => {
    if (divisionId) {
      return;
    }

    if (!filters.tournamentId) {
      return;
    }

    void getDivisionsByFiltersRef.current({
      tournamentId: filters.tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [divisionId, filters.tournamentId]);

  const fetchStages = useCallback(
    async (
      activeFilters: IStageListFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getStagesByFiltersRef.current(
        divisionId
          ? {
              divisionId,
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
          : {
              ...activeFilters,
              pageNumber: activePaginationModel.page + 1,
              pageSize: activePaginationModel.pageSize,
            }
      );
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    [divisionId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchStages(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchStages, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as IStageListFilters;

    if (name === 'divisionId') {
      updated.divisionId = (value as GUID) || undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'tournamentId') {
      updated.tournamentId = (value as GUID) || undefined;
      updated.divisionId = undefined;
      setDebouncedFilters(updated);
    }

    if (name === 'isActive') {
      updated.isActive =
        value === '' ? undefined : value.toLowerCase() === 'true';
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
    (row: IStageResponse) => {
      navigate(APP_ROUTES.panelStage.build(row.slug));
    },
    [navigate]
  );

  const handleDelete = useCallback(
    async (row: IStageResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar esta fase?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteStagesById(row.id);
      await notifySuccess({
        title: '¡Eliminada!',
        text: 'La fase ha sido eliminada.',
      });
    },
    [deleteStagesById]
  );

  const stageActions = useMemo<TableRowAction<IStageResponse>[]>(
    () => [
      {
        label: 'Ver partidos',
        color: 'info',
        icon: <VisibilityIcon fontSize="small" />,
        onClick: handleView,
      },
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: handleDelete,
      },
    ],
    [handleDelete, handleView]
  );

  const columns: GridColDef<IStageResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IStageResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1.2,
        minWidth: 180,
      },
      {
        field: 'stageType',
        headerName: 'Tipo',
        flex: 1,
        minWidth: 140,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => translateStageType(params.row.stageType),
      },
      {
        field: 'startDate',
        headerName: 'Inicio',
        flex: 0.8,
        minWidth: 120,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => formatDate(params.row.startDate),
      },
      {
        field: 'endDate',
        headerName: 'Fin',
        flex: 0.8,
        minWidth: 120,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => formatDate(params.row.endDate),
      },
      {
        field: 'isActive',
        headerName: 'Activa',
        flex: 0.6,
        minWidth: 90,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => (params.row.isActive ? 'Sí' : 'No'),
      },
    ];

    return [
      ...baseColumns,
      buildActionsColumn(stageActions, {
        align: 'center',
        headerAlign: 'center',
      }),
    ];
  }, [stageActions]);

  // Scope the rendered rows to the current division. The `stages` context is
  // shared across every list/detail that reads it, so right after navigating
  // between divisions it can still hold the previous division's stages until
  // this view's refetch resolves — filtering by `divisionId` guarantees a fresh
  // list never shows another division's stale rows.
  const rows = useMemo(
    () =>
      [...(stages ?? [])]
        .filter(stage => (divisionId ? stage.divisionId === divisionId : true))
        .sort(
          (a, b) =>
            (a.order ?? Number.MAX_SAFE_INTEGER) -
            (b.order ?? Number.MAX_SAFE_INTEGER)
        ),
    [stages, divisionId]
  );

  const hasActiveFilters = useMemo(
    () =>
      Boolean(filters.name) ||
      Boolean(filters.tournamentId) ||
      Boolean(filters.divisionId) ||
      filters.isActive !== undefined,
    [filters.divisionId, filters.isActive, filters.name, filters.tournamentId]
  );

  const canGenerateStages =
    Boolean(divisionId) &&
    showGenerateStagesButton &&
    !loading &&
    !hasActiveFilters &&
    rows.length === 0;

  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron fases para el filtro aplicado.'
    : emptyMessage;

  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const divisionOptions = useMemo(() => divisions ?? [], [divisions]);

  const handleGenerateStages = useCallback(async () => {
    if (!divisionId) {
      return;
    }

    setLoading(true);
    try {
      const generatedStages = await generateStagesAutomatically(divisionId);
      if (!generatedStages) {
        return;
      }

      const response = await getStagesByFilters({
        divisionId,
        pageNumber: paginationModel.page + 1,
        pageSize: paginationModel.pageSize,
      });
      setRowCount(response?.totalCount ?? 0);
      await notifySuccess({
        title: 'Éxito',
        text: 'Las fases fueron generadas correctamente.',
      });
    } finally {
      setLoading(false);
    }
  }, [
    divisionId,
    generateStagesAutomatically,
    getStagesByFilters,
    paginationModel.page,
    paginationModel.pageSize,
  ]);

  const handleCreateStage = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    const query = divisionId ? `?divisionId=${divisionId}` : '';
    navigate(`${APP_ROUTES.panelStageCreate}${query}`);
  }, [divisionId, navigate, onCreate]);

  const actionButtons = (
    <Stack direction="row" spacing={1}>
      {canGenerateStages && (
        <Button
          variant="contained"
          color="primary"
          startIcon={<SettingsSuggestIcon />}
          onClick={() => void handleGenerateStages()}
          disabled={loading}
        >
          Generar fases
        </Button>
      )}
      {createType && (
        <NewEntityButton
          gender="feminine"
          type={createType}
          onClick={handleCreateStage}
        />
      )}
    </Stack>
  );

  const filterBar = (
    <FilterBar
      onClear={hasActiveFilters ? handleClearFilters : undefined}
      ariaLabel="Filtros de fases"
    >
      <TextField
          label="Nombre"
          name="name"
          size="small"
          value={filters.name ?? ''}
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

        {!divisionId && (
          <TextField
            select
            label="Estado"
            name="isActive"
            size="small"
            value={
              filters.isActive === undefined
                ? ''
                : filters.isActive
                  ? 'true'
                  : 'false'
            }
            onChange={handleFilterChange}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">Todos</MenuItem>
            <MenuItem value="true">Activa</MenuItem>
            <MenuItem value="false">Inactiva</MenuItem>
          </TextField>
        )}

        {!divisionId && (
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

        {!divisionId && (
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
    </FilterBar>
  );

  const grid = (
    <Box sx={{ width: '100%' }}>
      <DataGrid
        rows={rows}
        columns={columns}
        loading={loading}
        getRowId={row => row.id}
        autoHeight
        disableRowSelectionOnClick
        disableColumnMenu
        localeText={dataGridLocaleText(noRowsMessage)}
        pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
        paginationModel={paginationModel}
        onPaginationModelChange={handlePaginationModelChange}
        paginationMode="server"
        rowCount={rowCount}
      />
    </Box>
  );

  if (wrapInCard) {
    return (
      <PageShell title={title} actions={actionButtons}>
        {filterBar}
        {grid}
      </PageShell>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      {(title || createType) && (
        <Stack
          direction="row"
          sx={{
            justifyContent: 'space-between',
            alignItems: 'center',
            mb: 2,
          }}
        >
          {title ? <Typography variant="h6">{title}</Typography> : <Box />}
          {actionButtons}
        </Stack>
      )}
      {filterBar}
      {grid}
    </Box>
  );
};

export default StagesPage;
