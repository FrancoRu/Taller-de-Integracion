import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Box,
  Card,
  CardContent,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  confirmDelete,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { TABLE_PAGE_SIZE_OPTIONS } from '@/modules/core/constants/pagination';
import { usePlayer } from '@/modules/player/hook/player.hook';
import FormButtons from '@/views/core/components/FormButtons';
import {
  IAddPlayerRequest,
  IPlayerResponse,
  IPutPlayerRequest,
  PlayerFiltered,
} from '@/modules/player/type/player.d';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import { useTeam } from '@/modules/team/hook/team.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_LONG_MS } from '@/modules/core/constants/constants';

type PlayersSearchFilters = Pick<
  PlayerFiltered,
  'names' | 'lastName' | 'documentNumber' | 'phoneNumber'
>;

type PlayerFormState = {
  firstName: string;
  secondName: string;
  lastName: string;
  documentNumber: string;
  birthDate: string;
  phoneNumber: string;
  socialSecurity: string;
  teamId: GUID | '';
};

const EMPTY_FILTERS: PlayersSearchFilters = {};

const INITIAL_PLAYER_FORM: PlayerFormState = {
  firstName: '',
  secondName: '',
  lastName: '',
  documentNumber: '',
  birthDate: '',
  phoneNumber: '',
  socialSecurity: '',
  teamId: '',
};

interface PlayersPageProps {
  teamId?: GUID;
  title?: string;
  emptyMessage?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
}

const getDateValue = (value?: Date) => {
  if (!value) {
    return '';
  }

  const dateValue = new Date(value);
  if (Number.isNaN(dateValue.getTime())) {
    return '';
  }

  return dateValue.toISOString().slice(0, 10);
};

const PlayersPage: React.FC<PlayersPageProps> = ({
  teamId,
  title = 'Jugadores',
  emptyMessage = 'No hay jugadores cargados.',
  wrapInCard = true,
  createType = 'Jugador',
  onCreate,
}) => {
  const navigate = useNavigate();
  const {
    players,
    addPlayer,
    putPlayerById,
    getPlayersByFilter,
    deletePlayerById,
  } = usePlayer();
  const { teams, getTeamsByFiltered } = useTeam();
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<PlayersSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<PlayersSearchFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });
  const getPlayersByFilterRef = useRef(getPlayersByFilter);

  useEffect(() => {
    getPlayersByFilterRef.current = getPlayersByFilter;
  }, [getPlayersByFilter]);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingPlayer, setEditingPlayer] = useState<IPlayerResponse | null>(
    null
  );
  const [playerForm, setPlayerForm] =
    useState<PlayerFormState>(INITIAL_PLAYER_FORM);

  const fetchPlayers = useCallback(
    async (
      activeFilters: PlayersSearchFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getPlayersByFilterRef.current(
        teamId
          ? {
              teamId,
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
    [teamId]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_LONG_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchPlayers(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchPlayers, paginationModel]);

  const loadTeamsForDropdown = useCallback(async () => {
    await getTeamsByFiltered({ pageSize: TABLE_ROWS_PER_PAGE });
  }, [getTeamsByFiltered]);

  const resetPlayerForm = useCallback(() => {
    setPlayerForm({
      ...INITIAL_PLAYER_FORM,
      teamId: teamId ?? '',
    });
  }, [teamId]);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    const updated = {
      ...filters,
      [name]: value || undefined,
    } as PlayersSearchFilters;

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
    (row: IPlayerResponse) => {
      navigate(APP_ROUTES.panelPlayer.build(row.id));
    },
    [navigate]
  );

  const handleEdit = useCallback(
    (row: IPlayerResponse) => {
      setEditingPlayer(row);
      setPlayerForm({
        firstName: row.firstName,
        secondName: row.secondName ?? '',
        lastName: row.lastName,
        documentNumber: row.documentNumber,
        birthDate: getDateValue(row.birthDate),
        phoneNumber: row.phoneNumber ?? '',
        socialSecurity: row.socialSecurity ?? '',
        teamId: row.teamId,
      });
      void loadTeamsForDropdown();
    },
    [loadTeamsForDropdown]
  );

  const handleDelete = useCallback(
    async (row: IPlayerResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar este jugador?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deletePlayerById(row.id);
      await notifySuccess({
        title: '¡Eliminado!',
        text: 'El jugador ha sido eliminado.',
      });
    },
    [deletePlayerById]
  );

  const playerActions = useMemo<TableRowAction<IPlayerResponse>[]>(
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

  const columns: GridColDef<IPlayerResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IPlayerResponse>[] = [
      {
        field: 'fullName',
        headerName: 'Jugador',
        flex: 1.4,
        minWidth: 220,
      },
      {
        field: 'documentNumber',
        headerName: 'Documento',
        flex: 0.8,
        minWidth: 140,
      },
      {
        field: 'phoneNumber',
        headerName: 'Teléfono',
        flex: 0.9,
        minWidth: 140,
        renderCell: params => params.row.phoneNumber || '—',
      },
      {
        field: 'socialSecurity',
        headerName: 'Obra social',
        flex: 1,
        minWidth: 160,
        renderCell: params => params.row.socialSecurity || '—',
      },
    ];

    return [...baseColumns, buildActionsColumn(playerActions)];
  }, [playerActions]);

  const rows = useMemo(() => players ?? [], [players]);
  const teamOptions = useMemo(() => teams ?? [], [teams]);

  const hasActiveFilters = useMemo(
    () =>
      Boolean(filters.names) ||
      Boolean(filters.lastName) ||
      Boolean(filters.documentNumber) ||
      Boolean(filters.phoneNumber),
    [
      filters.documentNumber,
      filters.names,
      filters.lastName,
      filters.phoneNumber,
    ]
  );

  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron jugadores para el filtro aplicado.'
    : emptyMessage;

  const validatePlayerForm = (resolvedTeamId: GUID | '') => {
    if (
      !playerForm.firstName.trim() ||
      !playerForm.secondName.trim() ||
      !playerForm.lastName.trim() ||
      !playerForm.documentNumber.trim() ||
      !playerForm.birthDate.trim() ||
      !playerForm.phoneNumber.trim() ||
      !playerForm.socialSecurity.trim()
    ) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre, segundo nombre, apellido, documento, fecha de nacimiento, teléfono y seguro social son obligatorios.',
      });
      return false;
    }

    if (!resolvedTeamId) {
      void notifyWarning({
        title: 'Equipo requerido',
        text: 'Debe seleccionar un equipo.',
      });
      return false;
    }

    return true;
  };

  const handleCreatePlayer = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    resetPlayerForm();
    if (!teamId) {
      void loadTeamsForDropdown();
    }
    setIsCreateModalOpen(true);
  }, [loadTeamsForDropdown, onCreate, resetPlayerForm, teamId]);

  const handleCreateSubmit = async () => {
    const resolvedTeamId = (teamId ?? playerForm.teamId) as GUID | '';

    if (!validatePlayerForm(resolvedTeamId)) {
      return;
    }

    setSubmitting(true);
    const payload: IAddPlayerRequest = {
      firstName: playerForm.firstName.trim(),
      secondName: playerForm.secondName.trim(),
      lastName: playerForm.lastName.trim(),
      documentNumber: playerForm.documentNumber.trim(),
      birthDate: new Date(playerForm.birthDate),
      phoneNumber: playerForm.phoneNumber.trim(),
      socialSecurity: playerForm.socialSecurity.trim(),
      teamId: resolvedTeamId as GUID,
    };

    const createdPlayer = await addPlayer(payload);
    setSubmitting(false);

    if (!createdPlayer) {
      return;
    }

    setIsCreateModalOpen(false);
    resetPlayerForm();
    await fetchPlayers(debouncedFilters, paginationModel);
    await notifySuccess({
      title: 'Jugador creado',
      text: 'El jugador se creó correctamente.',
    });
  };

  const handleEditSubmit = async () => {
    if (!editingPlayer) {
      return;
    }

    if (!validatePlayerForm(playerForm.teamId)) {
      return;
    }

    setSubmitting(true);
    const payload: IPutPlayerRequest = {
      firstName: playerForm.firstName.trim(),
      secondName: playerForm.secondName.trim(),
      lastName: playerForm.lastName.trim(),
      documentNumber: playerForm.documentNumber.trim(),
      birthDate: playerForm.birthDate
        ? new Date(playerForm.birthDate)
        : undefined,
      phoneNumber: playerForm.phoneNumber.trim() || undefined,
      socialSecurity: playerForm.socialSecurity.trim() || undefined,
      teamId: playerForm.teamId as GUID,
    };

    const updatedPlayer = await putPlayerById(editingPlayer.id, payload);
    setSubmitting(false);

    if (!updatedPlayer) {
      return;
    }

    setEditingPlayer(null);
    resetPlayerForm();
    await fetchPlayers(debouncedFilters, paginationModel);
    await notifySuccess({
      title: 'Jugador actualizado',
      text: 'El jugador se actualizó correctamente.',
    });
  };

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
          <NewEntityButton type={createType} onClick={handleCreatePlayer} />
        </Stack>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{
        mb: 2
      }}>
        <TextField
          label="Nombre"
          name="names"
          size="small"
          value={filters.names ?? ''}
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
          label="Apellido"
          name="lastName"
          size="small"
          value={filters.lastName ?? ''}
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
          label="Documento"
          name="documentNumber"
          size="small"
          value={filters.documentNumber ?? ''}
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
          label="Teléfono"
          name="phoneNumber"
          size="small"
          value={filters.phoneNumber ?? ''}
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

      <Dialog
        open={isCreateModalOpen}
        onClose={() => !submitting && setIsCreateModalOpen(false)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Nuevo jugador</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Nombre"
              value={playerForm.firstName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, firstName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Segundo nombre"
              value={playerForm.secondName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, secondName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Apellido"
              value={playerForm.lastName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, lastName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Documento"
              value={playerForm.documentNumber}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  documentNumber: e.target.value,
                }))
              }
              required
              fullWidth
            />
            <TextField
              label="Fecha de nacimiento"
              type="date"
              value={playerForm.birthDate}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, birthDate: e.target.value }))
              }
              fullWidth
              slotProps={{
                inputLabel: { shrink: true }
              }}
            />
            <TextField
              label="Teléfono"
              value={playerForm.phoneNumber}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  phoneNumber: e.target.value,
                }))
              }
              fullWidth
            />
            <TextField
              label="Obra social"
              value={playerForm.socialSecurity}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  socialSecurity: e.target.value,
                }))
              }
              fullWidth
            />
            {!teamId && (
              <FormControl fullWidth required>
                <InputLabel id="create-player-team-label">Equipo</InputLabel>
                <Select
                  labelId="create-player-team-label"
                  label="Equipo"
                  value={playerForm.teamId}
                  onChange={e =>
                    setPlayerForm(prev => ({
                      ...prev,
                      teamId: e.target.value as GUID,
                    }))
                  }
                >
                  {teamOptions.map(teamOption => (
                    <MenuItem key={teamOption.id} value={teamOption.id}>
                      {teamOption.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={() => {
              setIsCreateModalOpen(false);
              resetPlayerForm();
            }}
            onConfirm={() => void handleCreateSubmit()}
            confirmLabel="Crear"
            disabled={submitting}
          />
        </DialogActions>
      </Dialog>

      <Dialog
        open={Boolean(editingPlayer)}
        onClose={() => !submitting && setEditingPlayer(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Editar jugador</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Nombre"
              value={playerForm.firstName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, firstName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Segundo nombre"
              value={playerForm.secondName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, secondName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Apellido"
              value={playerForm.lastName}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, lastName: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Documento"
              value={playerForm.documentNumber}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  documentNumber: e.target.value,
                }))
              }
              required
              fullWidth
            />
            <TextField
              label="Fecha de nacimiento"
              type="date"
              value={playerForm.birthDate}
              onChange={e =>
                setPlayerForm(prev => ({ ...prev, birthDate: e.target.value }))
              }
              fullWidth
              slotProps={{
                inputLabel: { shrink: true }
              }}
            />
            <TextField
              label="Teléfono"
              value={playerForm.phoneNumber}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  phoneNumber: e.target.value,
                }))
              }
              fullWidth
            />
            <TextField
              label="Obra social"
              value={playerForm.socialSecurity}
              onChange={e =>
                setPlayerForm(prev => ({
                  ...prev,
                  socialSecurity: e.target.value,
                }))
              }
              fullWidth
            />
            <FormControl fullWidth required>
              <InputLabel id="edit-player-team-label">Equipo</InputLabel>
              <Select
                labelId="edit-player-team-label"
                label="Equipo"
                value={playerForm.teamId}
                onChange={e =>
                  setPlayerForm(prev => ({
                    ...prev,
                    teamId: e.target.value as GUID,
                  }))
                }
              >
                {teamOptions.map(teamOption => (
                  <MenuItem key={teamOption.id} value={teamOption.id}>
                    {teamOption.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={() => {
              setEditingPlayer(null);
              resetPlayerForm();
            }}
            onConfirm={() => void handleEditSubmit()}
            confirmLabel="Guardar"
            disabled={submitting}
          />
        </DialogActions>
      </Dialog>
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

export default PlayersPage;
