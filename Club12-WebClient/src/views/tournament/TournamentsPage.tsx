import React, { useCallback, useEffect, useMemo, useState } from 'react';
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
import {
  buildActionsColumn,
  TableRowAction,
} from '@/views/core/components/TableRowActions';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import { useTournament } from '../../modules/tournament/hook/tournament.hook';
import {
  ITournamentFiltered,
  ITournamentResponse,
} from '../../modules/tournament/type/tournament';
import { useAuth } from '../../modules/auth/hook/auth.hook';
import { UserRolesType } from '../../modules/core/enum/user/userRolesType';
import LoadingIndicator from '../core/components/LoadingIndicator';
import { useNavigate } from 'react-router-dom';

const EMPTY_FILTERS: ITournamentFiltered = {};

const TournamentsPage: React.FC = () => {
  const { tournaments, getAllTournamentsByFilter, deleteTournamentById } =
    useTournament();
  const { role } = useAuth();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<ITournamentFiltered>(EMPTY_FILTERS);

  const canLoadTournaments = useMemo(
    () =>
      role === UserRolesType.Admin ||
      role === UserRolesType.Owner ||
      role === UserRolesType.TournamentManager,
    [role]
  );

  const formatDate = (value: Date | string): string => {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return '—';
    }

    return parsed.toLocaleDateString('es-AR');
  };

  const handleView = useCallback(
    (row: ITournamentResponse) => {
      navigate(`/panel/torneos/${row.id}`);
    },
    [navigate]
  );

  const handleEdit = useCallback((_row: ITournamentResponse) => {
    // Pending panel route for tournament edit by id.
  }, []);

  const handleDelete = useCallback(
    async (row: ITournamentResponse) => {
      const result = await Swal.fire({
        title: '¿Está usted seguro de querer eliminar este torneo?',
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

      await deleteTournamentById(row.id);
      await Swal.fire({
        title: '¡Eliminado!',
        text: 'El torneo ha sido eliminado.',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
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
        renderCell: params => formatDate(params.row.teamRegistrationDeadline),
      },
      {
        field: 'startDate',
        headerName: 'Inicio',
        flex: 1,
        minWidth: 120,
        renderCell: params => formatDate(params.row.startDate),
      },
      {
        field: 'minTeams',
        headerName: 'Mín',
        flex: 0.5,
        minWidth: 70,
      },
      {
        field: 'maxTeams',
        headerName: 'Máx',
        flex: 0.5,
        minWidth: 70,
      },
      {
        field: 'isFinished',
        headerName: 'Estado',
        flex: 0.8,
        minWidth: 110,
        renderCell: params => (params.row.isFinished ? 'Finalizado' : 'Activo'),
      },
    ];

    return [...baseColumns, buildActionsColumn(tournamentActions)];
  }, [tournamentActions]);

  const fetchTournaments = useCallback(
    async (activeFilters: ITournamentFiltered) => {
      if (!canLoadTournaments) {
        return;
      }

      setLoading(true);
      await getAllTournamentsByFilter(activeFilters);
      setLoading(false);
    },
    [canLoadTournaments, getAllTournamentsByFilter]
  );

  useEffect(() => {
    if (!canLoadTournaments) {
      return;
    }

    void fetchTournaments(EMPTY_FILTERS);
  }, [canLoadTournaments, fetchTournaments]);

  useEffect(() => {
    if (!canLoadTournaments) {
      navigate('/forbidden', { replace: true });
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
    void fetchTournaments(updated);
  };

  const rows = useMemo(() => tournaments ?? [], [tournaments]);

  const handleCreateTournament = useCallback(() => {
    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de torneos desde el panel aún no está implementada.',
      icon: 'info',
      confirmButtonColor: '#FD6B00',
    });
  }, []);

  if (!canLoadTournaments) {
    return null;
  }

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={2}
        >
          <Typography variant="h6">Torneos</Typography>
          <NewEntityButton type="Torneo" onClick={handleCreateTournament} />
        </Stack>

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
          <TextField
            label="Descripción"
            name="description"
            size="small"
            value={filters.description ?? ''}
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

        {loading ? (
          <LoadingIndicator />
        ) : (
          <Box sx={{ width: '100%' }}>
            <DataGrid
              rows={rows}
              columns={columns}
              getRowId={row => row.id}
              autoHeight
              disableRowSelectionOnClick
              disableColumnMenu
              pageSizeOptions={[10, 25, 50]}
              initialState={{
                pagination: { paginationModel: { pageSize: 10 } },
              }}
            />
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default TournamentsPage;
