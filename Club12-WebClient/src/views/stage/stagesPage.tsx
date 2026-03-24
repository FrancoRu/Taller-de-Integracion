import { useCallback, useEffect, useMemo, useState } from 'react';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import {
  Box,
  Card,
  CardContent,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import { useNavigate } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { IStageResponse, StageFiltered } from '@/modules/stage/type/stage.d';
import {
  buildActionsColumn,
  TableRowAction,
} from '../core/components/TableRowActions';
import NewEntityButton from '../core/components/NewEntityButton';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '../core/MUI/icons/icons';

interface StagesPageProps {
  divisionId?: GUID;
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

type StageSearchFilters = Pick<StageFiltered, 'name'>;

const EMPTY_FILTERS: StageSearchFilters = {};

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

const formatStageType = (value: string) =>
  value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace('Final', 'Final')
    .trim();

const StagesPage: React.FC<StagesPageProps> = ({
  divisionId,
  emptyMessage = 'No hay fases cargadas.',
  title = 'Fases',
  wrapInCard = false,
  createType = 'Fase',
  onCreate,
}) => {
  const navigate = useNavigate();
  const { stages, getStagesByFilters, deleteStagesById } = useStage();
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<StageSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<StageSearchFilters>(EMPTY_FILTERS);

  const fetchStages = useCallback(
    async (activeFilters: StageSearchFilters) => {
      setLoading(true);
      await getStagesByFilters(
        divisionId
          ? {
              divisionId,
              ...activeFilters,
            }
          : activeFilters
      );
      setLoading(false);
    },
    [divisionId, getStagesByFilters]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchStages(debouncedFilters);
  }, [debouncedFilters, fetchStages]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as StageSearchFilters;

    setFilters(updated);
  };

  const handleView = useCallback(
    (row: IStageResponse) => {
      navigate(`/panel/fases/${row.id}`);
    },
    [navigate]
  );

  const handleEdit = useCallback((_row: IStageResponse) => {
    // Pending panel route for stage edit by id.
  }, []);

  const handleDelete = useCallback(
    async (row: IStageResponse) => {
      const result = await Swal.fire({
        title: '¿Está usted seguro de querer eliminar esta fase?',
        text: '¡Usted no podrá revertir este cambio!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#FD6B00',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
      });

      if (!result.isConfirmed) {
        return;
      }

      await deleteStagesById(row.id);
      await Swal.fire({
        title: '¡Eliminada!',
        text: 'La fase ha sido eliminada.',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
      });
    },
    [deleteStagesById]
  );

  const stageActions = useMemo<TableRowAction<IStageResponse>[]>(
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
        renderCell: params => formatStageType(params.row.stageType),
      },
      {
        field: 'order',
        headerName: 'Orden',
        flex: 0.5,
        minWidth: 90,
      },
      {
        field: 'startDate',
        headerName: 'Inicio',
        flex: 0.8,
        minWidth: 120,
        renderCell: params => formatDate(params.row.startDate),
      },
      {
        field: 'endDate',
        headerName: 'Fin',
        flex: 0.8,
        minWidth: 120,
        renderCell: params => formatDate(params.row.endDate),
      },
      {
        field: 'isActive',
        headerName: 'Activa',
        flex: 0.6,
        minWidth: 90,
        renderCell: params => (params.row.isActive ? 'Sí' : 'No'),
      },
    ];

    return [...baseColumns, buildActionsColumn(stageActions)];
  }, [stageActions]);

  const rows = useMemo(() => stages ?? [], [stages]);
  const hasActiveFilters = useMemo(() => Boolean(filters.name), [filters.name]);
  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron fases para el filtro aplicado.'
    : emptyMessage;

  const handleCreateStage = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de fases desde esta vista aún no está implementada.',
      icon: 'info',
      confirmButtonColor: '#FD6B00',
    });
  }, [onCreate]);

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
          <NewEntityButton
            gender="feminine"
            type={createType}
            onClick={handleCreateStage}
          />
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
          pageSizeOptions={[10, 25, 50]}
          initialState={{
            pagination: { paginationModel: { pageSize: 10 } },
          }}
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

export default StagesPage;
