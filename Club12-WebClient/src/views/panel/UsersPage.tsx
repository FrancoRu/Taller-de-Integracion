import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
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
import Swal from 'sweetalert2';
import {
  buildActionsColumn,
  TableRowAction,
} from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import { useUser } from '@/modules/user/hook/user.hook';
import { UserFilterRequest, UserResponse } from '@/modules/user/type/user';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';

const ROLE_LABELS: Record<UserRolesType, string> = {
  ADMIN: 'Admin',
  OWNER: 'Owner',
  TOURNAMENT_MANAGER: 'Responsable del Torneo',
  TEAM_MANAGER: 'Responsable de Equipo',
  GUEST: 'Invitado',
};

const EMPTY_FILTERS: UserFilterRequest = {};

const UsersPage: React.FC = () => {
  const { users, getAllUsers, deleteUser } = useUser();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [filters, setFilters] = useState<UserFilterRequest>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getAllUsersRef = useRef(getAllUsers);

  useEffect(() => {
    getAllUsersRef.current = getAllUsers;
  }, [getAllUsers]);

  const handleView = useCallback(
    (row: UserResponse) => {
      navigate(`/panel/usuarios/${row.userId}`);
    },
    [navigate]
  );

  const handleEdit = useCallback(
    (row: UserResponse) => {
      navigate(`/panel/usuarios/${row.userId}/editar`);
    },
    [navigate]
  );

  const handleDelete = useCallback(
    async (row: UserResponse) => {
      const result = await Swal.fire({
        title: '¿Eliminar usuario?',
        text: `Se eliminará a "${row.username}" de forma permanente.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#d32f2f',
        cancelButtonColor: '#6e6e6e',
      });

      if (!result.isConfirmed) {
        return;
      }

      const deleted = await deleteUser(row.userId);
      if (deleted) {
        await Swal.fire({
          title: 'Eliminado',
          text: 'El usuario fue eliminado correctamente.',
          icon: 'success',
          confirmButtonColor: '#FD6B00',
        });
      }
    },
    [deleteUser]
  );

  const userActions = useMemo<TableRowAction<UserResponse>[]>(
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

  const columns: GridColDef<UserResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<UserResponse>[] = [
      {
        field: 'username',
        headerName: 'Usuario',
        flex: 1,
        minWidth: 140,
      },
      {
        field: 'email',
        headerName: 'Email',
        flex: 1.5,
        minWidth: 200,
      },
      {
        field: 'phoneNumber',
        headerName: 'Teléfono',
        flex: 1,
        minWidth: 130,
        renderCell: params => params.row.phoneNumber ?? '—',
      },
      {
        field: 'role',
        headerName: 'Rol',
        flex: 1,
        minWidth: 160,
        renderCell: params =>
          ROLE_LABELS[params.row.role as UserRolesType] ?? params.row.role,
      },
    ];

    return [...baseColumns, buildActionsColumn(userActions)];
  }, [userActions]);

  const fetchUsers = useCallback(
    async (
      activeFilters: UserFilterRequest,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      await getAllUsersRef.current({
        ...activeFilters,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchUsers(filters, paginationModel);
  }, [fetchUsers, filters, paginationModel]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = { ...filters, [name]: value || undefined };
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

  const rows = useMemo(() => users ?? [], [users]);

  return (
    <Card>
      <CardContent>
        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          mb={2}
        >
          <Typography variant="h6">Usuarios</Typography>
          <NewEntityButton
            type="Usuario"
            onClick={() => navigate('/panel/usuarios/crear')}
          />
        </Stack>

        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} mb={2}>
          <TextField
            label="Usuario"
            name="username"
            size="small"
            value={filters.username ?? ''}
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
            label="Email"
            name="email"
            size="small"
            value={filters.email ?? ''}
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
            select
            label="Rol"
            name="role"
            size="small"
            value={filters.role ?? ''}
            onChange={handleFilterChange}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">Todos</MenuItem>
            {Object.values(UserRolesType)
              .filter(
                role =>
                  role !== UserRolesType.Guest && role !== UserRolesType.Admin
              )
              .map(role => (
                <MenuItem key={role} value={role}>
                  {ROLE_LABELS[role]}
                </MenuItem>
              ))}
          </TextField>
        </Stack>

        {loading ? (
          <LoadingIndicator />
        ) : (
          <Box sx={{ width: '100%' }}>
            <DataGrid
              rows={rows}
              columns={columns}
              getRowId={row => row.userId}
              autoHeight
              disableRowSelectionOnClick
              disableColumnMenu
              pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
              paginationModel={paginationModel}
              onPaginationModelChange={handlePaginationModelChange}
            />
          </Box>
        )}
      </CardContent>
    </Card>
  );
};

export default UsersPage;
