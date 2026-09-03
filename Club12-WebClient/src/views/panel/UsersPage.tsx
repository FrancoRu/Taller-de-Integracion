import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Button,
  Chip,
  InputAdornment,
  MenuItem,
  TextField,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { confirmAction, notifySuccess } from '@/modules/core/utils/confirmDialog';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import TableScrollBox from '@/views/core/components/TableScrollBox';
import { TableSkeleton } from '@/views/core/components/skeletons';
import {
  DeleteIcon,
  LockIcon,
  LockOpenIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import { useUser } from '@/modules/user/hook/user.hook';
import { UserFilterRequest, UserResponse } from '@/modules/user/type/user';
import {
  USER_ROLE_LABELS,
  UserRolesType,
} from '@/modules/core/enum/user/userRolesType';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const EMPTY_FILTERS: UserFilterRequest = {};

const UsersPage: React.FC = () => {
  const { users, getAllUsers, deleteUser, setUserActive } = useUser();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
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
      navigate(APP_ROUTES.panelUser.build(row.userId));
    },
    [navigate]
  );

  const fetchUsers = useCallback(
    async (
      activeFilters: UserFilterRequest,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getAllUsersRef.current({
        ...activeFilters,
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
      });
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  const handleDelete = useCallback(
    async (row: UserResponse) => {
      const confirmed = await confirmAction({
        title: '¿Eliminar usuario?',
        text: `Se eliminará a "${row.username}" de forma permanente.`,
        confirmButtonText: 'Eliminar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#d32f2f',
        cancelButtonColor: '#6e6e6e',
      });

      if (!confirmed) {
        return;
      }

      const deleted = await deleteUser(row.userId);
      if (deleted) {
        await fetchUsers(filters, paginationModel);
        await notifySuccess({
          title: 'Eliminado',
          text: 'El usuario fue eliminado correctamente.',
        });
      }
    },
    [deleteUser, fetchUsers, filters, paginationModel]
  );

  const handleToggleActive = useCallback(
    async (row: UserResponse) => {
      const deactivating = row.isActive;
      const confirmed = await confirmAction({
        title: deactivating ? '¿Desactivar usuario?' : '¿Activar usuario?',
        text: deactivating
          ? `"${row.username}" no podrá iniciar sesión hasta reactivarlo.`
          : `"${row.username}" podrá volver a iniciar sesión.`,
        confirmButtonText: deactivating ? 'Desactivar' : 'Activar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: deactivating ? '#d32f2f' : '#2e7d32',
        cancelButtonColor: '#6e6e6e',
      });

      if (!confirmed) {
        return;
      }

      const updated = await setUserActive(row.userId, !row.isActive);
      if (updated) {
        await fetchUsers(filters, paginationModel);
        await notifySuccess({
          title: deactivating ? 'Usuario desactivado' : 'Usuario activado',
        });
      }
    },
    [setUserActive, fetchUsers, filters, paginationModel]
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
        label: 'Desactivar',
        color: 'warning',
        icon: <LockIcon fontSize="small" />,
        onClick: handleToggleActive,
        hidden: row => !row.isActive,
      },
      {
        label: 'Activar',
        color: 'success',
        icon: <LockOpenIcon fontSize="small" />,
        onClick: handleToggleActive,
        hidden: row => row.isActive,
      },
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: handleDelete,
      },
    ],
    [handleDelete, handleToggleActive, handleView]
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
        align: 'center',
        headerAlign: 'center',
        renderCell: params =>
          USER_ROLE_LABELS[params.row.role as UserRolesType] ?? params.row.role,
      },
      {
        field: 'isActive',
        headerName: 'Estado',
        flex: 0.7,
        minWidth: 110,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => (
          <Chip
            size="small"
            label={params.row.isActive ? 'Activo' : 'Inactivo'}
            color={params.row.isActive ? 'success' : 'default'}
            variant={params.row.isActive ? 'filled' : 'outlined'}
          />
        ),
      },
    ];

    return [
      ...baseColumns,
      buildActionsColumn(userActions, {
        align: 'center',
        headerAlign: 'center',
      }),
    ];
  }, [userActions]);

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

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
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
    <PageShell
      title="Usuarios"
      actions={
        <>
          <Button
            variant="outlined"
            onClick={() => navigate(APP_ROUTES.panelUserInvite)}
          >
            Invitar usuario
          </Button>
          <NewEntityButton
            type="Usuario"
            onClick={() => navigate(APP_ROUTES.panelUserCreate)}
          />
        </>
      }
    >
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de usuarios">
        <TextField
          label="Usuario"
          name="username"
            size="small"
            value={filters.username ?? ''}
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
            label="Email"
            name="email"
            size="small"
            value={filters.email ?? ''}
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
              .filter(role => role !== UserRolesType.Guest)
              .map(role => (
                <MenuItem key={role} value={role}>
                  {USER_ROLE_LABELS[role]}
                </MenuItem>
              ))}
          </TextField>
      </FilterBar>

      {loading ? (
        <TableSkeleton columns={6} />
      ) : (
        <TableScrollBox>
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
            paginationMode="server"
            rowCount={rowCount}
          />
        </TableScrollBox>
      )}
    </PageShell>
  );
};

export default UsersPage;
