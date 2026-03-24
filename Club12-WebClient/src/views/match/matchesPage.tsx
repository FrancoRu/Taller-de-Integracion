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
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse, MatchFiltered } from '@/modules/match/type/match';
import {
  buildActionsColumn,
  TableRowAction,
} from '../core/components/TableRowActions';
import NewEntityButton from '../core/components/NewEntityButton';
import TeamLogo from '../core/components/TeamLogo';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '../core/MUI/icons/icons';

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
  'homeTeamName' | 'visitorTeamName'
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
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<MatchesSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<MatchesSearchFilters>(EMPTY_FILTERS);

  const fetchMatches = useCallback(
    async (activeFilters: MatchesSearchFilters) => {
      setLoading(true);
      await getMatchByFilter(
        stageId
          ? {
              stageId,
              ...activeFilters,
            }
          : activeFilters
      );
      setLoading(false);
    },
    [getMatchByFilter, stageId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchMatches(debouncedFilters);
  }, [debouncedFilters, fetchMatches]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as MatchesSearchFilters;

    setFilters(updated);
  };

  const handleView = useCallback(
    (row: IMatchResponse) => {
      navigate(`/panel/partidos/${row.id}`);
    },
    [navigate]
  );

  const handleEdit = useCallback((_row: IMatchResponse) => {
    // Pending panel route for match edit by id.
  }, []);

  const handleDelete = useCallback(
    async (row: IMatchResponse) => {
      const result = await Swal.fire({
        title: '¿Está usted seguro de querer eliminar este partido?',
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

      await deleteMatchById(row.id);
      await Swal.fire({
        title: '¡Eliminado!',
        text: 'El partido ha sido eliminado.',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
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
            <Stack direction="row" alignItems="center" spacing={1}>
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
            <Stack direction="row" alignItems="center" spacing={1}>
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
  const hasActiveFilters = useMemo(
    () => Boolean(filters.homeTeamName) || Boolean(filters.visitorTeamName),
    [filters.homeTeamName, filters.visitorTeamName]
  );
  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron partidos para el filtro aplicado.'
    : emptyMessage;

  const handleCreateMatch = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de partidos desde esta vista aún no está implementada.',
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
          <NewEntityButton type={createType} onClick={handleCreateMatch} />
        </Stack>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
        <TextField
          label="Equipo local"
          name="homeTeamName"
          size="small"
          value={filters.homeTeamName ?? ''}
          onChange={handleFilterChange}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          }}
        />
        <TextField
          label="Equipo visitante"
          name="visitorTeamName"
          size="small"
          value={filters.visitorTeamName ?? ''}
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

export default MatchesPage;
