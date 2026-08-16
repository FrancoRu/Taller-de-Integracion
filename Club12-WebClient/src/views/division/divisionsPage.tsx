import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Box,
  Card,
  CardContent,
  InputAdornment,
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
import { useDivision } from '@/modules/division/hook/division.hook';
import {
  DivisionFiltered,
  IDivisionResponse,
} from '@/modules/division/type/division';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';

interface DivisionsPageProps {
  tournamentId?: GUID;
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

type DivisionSearchFilters = Pick<DivisionFiltered, 'name'>;

const EMPTY_FILTERS: DivisionSearchFilters = {};

const DivisionsPage: React.FC<DivisionsPageProps> = ({
  tournamentId,
  emptyMessage = 'No hay divisiones cargadas.',
  title = 'Divisiones',
  wrapInCard = false,
  createType = 'División',
  onCreate,
}) => {
  const navigate = useNavigate();
  const { divisions, getDivisionsByFilters, deleteDivisionsById } =
    useDivision();
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<DivisionSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<DivisionSearchFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getDivisionsByFiltersRef = useRef(getDivisionsByFilters);

  useEffect(() => {
    getDivisionsByFiltersRef.current = getDivisionsByFilters;
  }, [getDivisionsByFilters]);

  const fetchDivisions = useCallback(
    async (
      activeFilters: DivisionSearchFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      await getDivisionsByFiltersRef.current(
        tournamentId
          ? {
              tournamentId,
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
      setLoading(false);
    },
    [tournamentId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchDivisions(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchDivisions, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as DivisionSearchFilters;

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
    (row: IDivisionResponse) => {
      navigate(APP_ROUTES.panelDivision.build(row.id));
    },
    [navigate]
  );

  /** No-op until the panel route for editing a division by id is implemented. */
  const handleEdit = useCallback((_row: IDivisionResponse) => {}, []);

  const handleDelete = useCallback(
    async (row: IDivisionResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar esta división?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteDivisionsById(row.id);
      await notifySuccess({
        title: '¡Eliminada!',
        text: 'La división ha sido eliminada.',
      });
    },
    [deleteDivisionsById]
  );

  const divisionActions = useMemo<TableRowAction<IDivisionResponse>[]>(
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

  const columns: GridColDef<IDivisionResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IDivisionResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1.2,
        minWidth: 180,
      },
      {
        field: 'isFinished',
        headerName: 'Estado',
        flex: 0.8,
        minWidth: 120,
        renderCell: params => (params.row.isFinished ? 'Finalizada' : 'Activa'),
      },
      {
        field: 'positions',
        headerName: 'Equipos',
        flex: 0.6,
        minWidth: 100,
        sortable: false,
        renderCell: params => params.row.positions?.length ?? 0,
      },
    ];

    return [...baseColumns, buildActionsColumn(divisionActions)];
  }, [divisionActions]);

  const rows = useMemo(() => divisions ?? [], [divisions]);
  const hasActiveFilters = useMemo(() => Boolean(filters.name), [filters.name]);
  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron divisiones para el filtro aplicado.'
    : emptyMessage;

  const handleCreateDivision = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    navigate(APP_ROUTES.panelDivisionCreate);
  }, [onCreate, navigate]);

  const content = (
    <>
      {(title || createType) && (
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={2}
        >
          {title ? <Typography variant="h6">{title}</Typography> : <Box />}
          <NewEntityButton type={createType} onClick={handleCreateDivision} />
        </Stack>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
        <TextField
          label="Nombre"
          name="name"
          size="small"
          value={filters.name ?? ''}
          onChange={handleFilterChange}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
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

export default DivisionsPage;
