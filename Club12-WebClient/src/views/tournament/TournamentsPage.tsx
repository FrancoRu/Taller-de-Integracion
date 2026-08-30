import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { Box, Chip, InputAdornment, TextField } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import {
  confirmDelete,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import {
  DeleteIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import {
  ITournamentFiltered,
  ITournamentResponse,
} from '@/modules/tournament/type/tournament';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useNavigate } from 'react-router-dom';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import {
  TOURNAMENT_STATUS_LABEL,
  TOURNAMENT_STATUS_COLOR,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { formatDateAr } from '@/modules/core/utils/formatDate';

const EMPTY_FILTERS: ITournamentFiltered = {};

const TournamentsPage: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter, deleteTournamentById } =
    useTournament();
  const { role } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<ITournamentFiltered>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getAllTournamentsByFilterRef = useRef(getAllTournamentsByFilter);

  useEffect(() => {
    getAllTournamentsByFilterRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  const canLoadTournaments = useMemo(
    () => role === UserRolesType.Admin || role === UserRolesType.Owner,
    [role]
  );

  const handleView = useCallback(
    (row: ITournamentResponse) => {
      navigate(APP_ROUTES.panelTournamentDetail.build(row.slug));
    },
    [navigate]
  );

  const handleDelete = useCallback(
    async (row: ITournamentResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar este torneo?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteTournamentById(row.id);
      await notifySuccess({
        title: '¡Eliminado!',
        text: 'El torneo ha sido eliminado.',
      });
    },
    [deleteTournamentById]
  );

  const tournamentActions = useMemo<TableRowAction<ITournamentResponse>[]>(
    () => [
      {
        label: 'Ver',
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

  const columns: GridColDef<ITournamentResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<ITournamentResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1,
        minWidth: 170,
      },
      {
        field: 'description',
        headerName: 'Descripción',
        flex: 1.4,
        minWidth: 220,
      },
      {
        field: 'teamRegistrationDeadline',
        headerName: 'Cierre inscripción',
        flex: 1,
        minWidth: 150,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => formatDateAr(params.row.teamRegistrationDeadline),
      },
      {
        field: 'startDate',
        headerName: 'Inicio',
        flex: 1,
        minWidth: 120,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => formatDateAr(params.row.startDate),
      },
      {
        field: 'status',
        headerName: 'Estado',
        flex: 1,
        minWidth: 160,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => {
          const status = resolveTournamentStatus(params.row.status);
          return (
            <Chip
              size="small"
              label={TOURNAMENT_STATUS_LABEL[status]}
              color={TOURNAMENT_STATUS_COLOR[status]}
              variant={
                status === TournamentStatus.Scheduled ? 'outlined' : 'filled'
              }
            />
          );
        },
      },
    ];

    return [
      ...baseColumns,
      buildActionsColumn(tournamentActions, {
        align: 'center',
        headerAlign: 'center',
      }),
    ];
  }, [tournamentActions]);

  const fetchTournaments = useCallback(
    async (
      activeFilters: ITournamentFiltered,
      activePaginationModel: GridPaginationModel
    ) => {
      if (!canLoadTournaments) {
        return;
      }

      setLoading(true);
      const response = await getAllTournamentsByFilterRef.current({
        ...activeFilters,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    [canLoadTournaments]
  );

  useEffect(() => {
    if (!canLoadTournaments) {
      return;
    }

    void fetchTournaments(filters, paginationModel);
  }, [canLoadTournaments, fetchTournaments, filters, paginationModel]);

  useEffect(() => {
    if (!canLoadTournaments) {
      navigate(APP_ROUTES.forbidden, { replace: true });
    }
  }, [canLoadTournaments, navigate]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    };

    setFilters(updated);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  };

  const hasActiveFilters = Boolean(filters.name) || Boolean(filters.description);

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

  const rows = useMemo(() => tournaments ?? [], [tournaments]);

  const handleCreateTournament = useCallback(() => {
    navigate(APP_ROUTES.panelTournamentWizard);
  }, [navigate]);

  if (!canLoadTournaments) {
    return null;
  }

  return (
    <PageShell
      title="Torneos"
      actions={<NewEntityButton type="Torneo" onClick={handleCreateTournament} />}
    >
      <FilterBar
        onClear={hasActiveFilters ? handleClearFilters : undefined}
        ariaLabel="Filtros de torneos"
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
            },
          }}
        />
        <TextField
          label="Descripción"
          name="description"
          size="small"
          value={filters.description ?? ''}
          onChange={handleFilterChange}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
        />
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
          localeText={dataGridLocaleText(
            hasActiveFilters
              ? 'No se encontraron torneos para el filtro aplicado.'
              : 'Todavía no hay torneos. Creá el primero para empezar.'
          )}
          pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
          paginationModel={paginationModel}
          onPaginationModelChange={handlePaginationModelChange}
          paginationMode="server"
          rowCount={rowCount}
        />
      </Box>
    </PageShell>
  );
};

export default TournamentsPage;
