import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  DataGrid,
  GridCellParams,
  GridColDef,
  GridPaginationModel,
  GridRenderEditCellParams,
  GridRowId,
  GridRowModes,
  GridRowModesModel,
  useGridApiContext,
} from '@mui/x-data-grid';
import {
  Box,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  InputAdornment,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import {
  formatArgentinePhone,
  formatDocumentNumber,
} from '@/modules/core/utils/validators';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { TABLE_PAGE_SIZE_OPTIONS } from '@/modules/core/constants/pagination';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { usePlayer } from '@/modules/player/hook/player.hook';
import FormButtons from '@/views/core/components/FormButtons';
import FieldInfoTooltip from '@/views/core/components/FieldInfoTooltip';
import TableScrollBox from '@/views/core/components/TableScrollBox';
import { IAddPlayerRequest, IPlayerResponse } from '@/modules/player/type/player.d';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import TableRowActions, { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import {
  CheckIcon,
  CloseIcon,
  DeleteIcon,
  MedicalInformationIcon,
  NumbersIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import { useTeam } from '@/modules/team/hook/team.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_LONG_MS } from '@/modules/core/constants/constants';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import HabilitacionBadge from '@/views/medicalRecord/HabilitacionBadge';
import PlayerMedicalRecordDialog from '@/views/medicalRecord/PlayerMedicalRecordDialog';
import { validatePlayerFields } from '@/views/player/players.types';
import type { PlayersSearchFilters } from '@/views/player/players.types';

/** Per-player medical / eligibility signal keyed by player id (HU-57/HU-62). */
export interface PlayerMedicalInfo {
  status?: MedicalRecordStatus | null;
  isHabilitado?: boolean;
}

const EMPTY_FILTERS: PlayersSearchFilters = {};

/** A row rendered by the roster grid — either a real, persisted player, or an
 * in-progress draft row being filled in directly in the table before it's
 * saved (replaces the old "Nuevo jugador" popup form). */
type PlayerRow = IPlayerResponse & { isNew?: boolean };

/** Shared 0-99 dorsal parsing/validation for both the standalone Dorsal
 * dialog and the dorsal field inside "Editar jugador". */
const parseDorsalValue = (
  value: string
): { success: true; jerseyNumber: number | null } | { success: false } => {
  const trimmed = value.trim();
  if (trimmed === '') {
    return { success: true, jerseyNumber: null };
  }

  const parsed = Number(trimmed);
  if (!Number.isInteger(parsed) || parsed < 0 || parsed > 99) {
    return { success: false };
  }

  return { success: true, jerseyNumber: parsed };
};

/** A draft row's birthDate is always a plain 'yyyy-MM-dd' string (what its
 * date input produces) — this is only ever called on draft rows, never on a
 * persisted player's row (whose birthDate may be a real Date/ISO string). */
const rowBirthDateValue = (row: PlayerRow): string =>
  row.birthDate ? String(row.birthDate).slice(0, 10) : '';

const validateDraftRow = (
  row: PlayerRow,
  resolvedTeamId: GUID | ''
): { title: string; text: string } | null =>
  validatePlayerFields(
    {
      firstName: row.firstName,
      secondName: row.secondName,
      lastName: row.lastName,
      documentNumber: row.documentNumber,
      birthDate: rowBirthDateValue(row),
      phoneNumber: row.phoneNumber,
      socialSecurity: row.socialSecurity,
    },
    resolvedTeamId
  );

/** A plain `<input type="date">` edit cell for the birthDate column. The
 * DataGrid's built-in `type: 'date'` column expects a real `Date` value and
 * round-trips awkwardly against the API's string shape, so this keeps the
 * same 'yyyy-MM-dd' string the rest of the create flow already works with. */
const BirthDateEditCell: React.FC<GridRenderEditCellParams<PlayerRow>> = props => {
  const { id, field, value } = props;
  const apiRef = useGridApiContext();

  return (
    <TextField
      type="date"
      value={typeof value === 'string' ? value : ''}
      onChange={e =>
        void apiRef.current.setEditCellValue({ id, field, value: e.target.value })
      }
      autoFocus
      fullWidth
      size="small"
      sx={{ px: 1 }}
      slotProps={{ inputLabel: { shrink: true } }}
    />
  );
};

interface PlayersPageProps {
  teamId?: GUID;
  title?: string;
  emptyMessage?: string;
  wrapInCard?: boolean;
  createType?: string;
  onCreate?: () => void;
  /**
   * When set, the roster shows the per-player habilitación badge and a
   * "ficha médica" action scoped to this tournament (HU-55/57/58). The medical
   * record is scoped to player + team + tournament, so both `teamId` and
   * `tournamentId` are required to enable it.
   */
  tournamentId?: GUID | null;
  /** Per-player medical/eligibility signal keyed by player id (from the roster). */
  medicalByPlayerId?: Map<GUID, PlayerMedicalInfo>;
  /** Per-player dorsal (jersey number) keyed by player id (from the roster, HU-54). */
  jerseyByPlayerId?: Map<GUID, number | null | undefined>;
  /** Called after a medical record is uploaded/reviewed so the roster can refresh. */
  onMedicalChange?: () => void;
  /** Bumped by the parent (e.g. after a CSV import) to force a re-fetch of
   * the roster list — needed because CSV import creates players outside
   * this component and only surfaces the partial create-response shape in
   * shared state otherwise. */
  refreshTrigger?: number;
}

const PlayersPage: React.FC<PlayersPageProps> = ({
  teamId,
  title = 'Jugadores',
  emptyMessage = 'No hay jugadores cargados.',
  wrapInCard = true,
  createType = 'Jugador',
  onCreate,
  tournamentId,
  medicalByPlayerId,
  jerseyByPlayerId,
  onMedicalChange,
  refreshTrigger,
}) => {
  const navigate = useNavigate();
  const {
    players,
    addPlayer,
    getPlayersByFilter,
    deletePlayerById,
    registerPlayerToTeam,
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

  const [draftRows, setDraftRows] = useState<PlayerRow[]>([]);
  const [rowModesModel, setRowModesModel] = useState<GridRowModesModel>({});
  const draftCounterRef = useRef(0);
  const [medicalPlayer, setMedicalPlayer] = useState<IPlayerResponse | null>(
    null
  );
  const [dorsalPlayer, setDorsalPlayer] = useState<IPlayerResponse | null>(
    null
  );
  const [dorsalValue, setDorsalValue] = useState('');

  const medicalEnabled = Boolean(teamId && tournamentId);
  // HU-54: assigning a dorsal is a season-roster operation, so it needs both a
  // team and a tournament in scope — same precondition as the medical record.
  const rosterEnabled = medicalEnabled;

  const currentDorsalFor = useCallback(
    (row: IPlayerResponse): number | null | undefined =>
      jerseyByPlayerId?.get(row.id) ?? row.jerseyNumber,
    [jerseyByPlayerId]
  );

  const handleOpenMedical = useCallback((row: IPlayerResponse) => {
    setMedicalPlayer(row);
  }, []);

  const handleOpenDorsal = useCallback(
    (row: IPlayerResponse) => {
      const dorsal = currentDorsalFor(row);
      setDorsalValue(
        dorsal === null || dorsal === undefined ? '' : String(dorsal)
      );
      setDorsalPlayer(row);
    },
    [currentDorsalFor]
  );

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
  }, [debouncedFilters, fetchPlayers, paginationModel, refreshTrigger]);

  const loadTeamsForDropdown = useCallback(async () => {
    await getTeamsByFiltered({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [getTeamsByFiltered]);

  // Also needed just to render the list's "Equipo" column — a player always
  // belongs to a team, so load the lookup once up front rather than only
  // when a draft row is added.
  useEffect(() => {
    void loadTeamsForDropdown();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const teamNameById = useMemo(
    () => new Map((teams ?? []).map(team => [team.id, team.name])),
    [teams]
  );
  const teamOptions = useMemo(() => teams ?? [], [teams]);

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

  const handleView = useCallback(
    (row: IPlayerResponse) => {
      navigate(APP_ROUTES.panelPlayer.build(row.slug));
    },
    [navigate]
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

      const result = await deletePlayerById(row.id);

      if (!result.success) {
        // Surface the backend integrity block (e.g. the player has statistics
        // or sanctions) with its exact Spanish message.
        await notifyError({
          title: 'No se pudo eliminar',
          text: result.errorMessage,
        });
        return;
      }

      await fetchPlayers(debouncedFilters, paginationModel);
      await notifySuccess({
        title: '¡Eliminado!',
        text: 'El jugador ha sido eliminado.',
      });
    },
    [deletePlayerById, fetchPlayers, debouncedFilters, paginationModel]
  );

  const playerActions = useMemo<TableRowAction<PlayerRow>[]>(
    () => [
      {
        label: 'Ver',
        color: 'info',
        icon: <VisibilityIcon fontSize="small" />,
        onClick: handleView,
      },
      {
        label: 'Ficha médica',
        color: 'success',
        icon: <MedicalInformationIcon fontSize="small" />,
        onClick: handleOpenMedical,
        hidden: !medicalEnabled,
      },
      {
        label: 'Dorsal',
        color: 'info',
        icon: <NumbersIcon fontSize="small" />,
        onClick: handleOpenDorsal,
        hidden: !rosterEnabled,
      },
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: handleDelete,
      },
    ],
    [
      handleDelete,
      handleOpenDorsal,
      handleOpenMedical,
      handleView,
      medicalEnabled,
      rosterEnabled,
    ]
  );

  const buildDraftRow = useCallback((): PlayerRow => {
    const id = `draft-${draftCounterRef.current++}` as GUID;
    return {
      id,
      slug: '',
      fullName: '',
      firstName: '',
      secondName: '',
      lastName: '',
      documentNumber: '',
      birthDate: '' as unknown as Date,
      phoneNumber: '',
      socialSecurity: '',
      teamId: (teamId ?? '') as GUID,
      isFederated: false,
      club: '',
      category: '',
      isNew: true,
    };
  }, [teamId]);

  const handleCreatePlayer = useCallback(() => {
    if (onCreate) {
      onCreate();
      return;
    }

    if (!teamId) {
      void loadTeamsForDropdown();
    }

    const draft = buildDraftRow();
    setDraftRows(prev => [draft, ...prev]);
    setRowModesModel(prev => ({
      ...prev,
      [draft.id]: { mode: GridRowModes.Edit, fieldToFocus: 'firstName' },
    }));
  }, [onCreate, teamId, loadTeamsForDropdown, buildDraftRow]);

  const handleDiscardDraftRow = useCallback((id: GridRowId) => {
    setRowModesModel(prev => ({
      ...prev,
      [id]: { mode: GridRowModes.View, ignoreModifications: true },
    }));
    setDraftRows(prev => prev.filter(row => row.id !== id));
  }, []);

  const handleSaveDraftRow = useCallback((id: GridRowId) => {
    setRowModesModel(prev => ({ ...prev, [id]: { mode: GridRowModes.View } }));
  }, []);

  const processRowUpdate = useCallback(
    async (newRow: PlayerRow): Promise<PlayerRow> => {
      const resolvedTeamId = (teamId ?? newRow.teamId) as GUID | '';
      const validationError = validateDraftRow(newRow, resolvedTeamId);
      if (validationError) {
        void notifyWarning(validationError);
        throw validationError;
      }

      setSubmitting(true);
      const payload: IAddPlayerRequest = {
        firstName: newRow.firstName.trim(),
        secondName: newRow.secondName?.trim() || undefined,
        lastName: newRow.lastName.trim(),
        documentNumber: newRow.documentNumber.trim(),
        birthDate: new Date(rowBirthDateValue(newRow)),
        phoneNumber: newRow.phoneNumber.trim(),
        socialSecurity: newRow.socialSecurity.trim(),
        teamId: resolvedTeamId as GUID,
      };

      const createdPlayer = await addPlayer(payload);
      setSubmitting(false);

      if (!createdPlayer) {
        // addPlayer already surfaced the failure via the global error
        // handler — throw so the row stays in edit mode and nothing typed
        // is lost.
        throw new Error('No se pudo crear el jugador.');
      }

      setDraftRows(prev => prev.filter(row => row.id !== newRow.id));
      await fetchPlayers(debouncedFilters, paginationModel);
      void notifySuccess({
        title: 'Jugador creado',
        text: 'El jugador se creó correctamente.',
      });

      return newRow;
    },
    [teamId, addPlayer, fetchPlayers, debouncedFilters, paginationModel]
  );

  const handleProcessRowUpdateError = useCallback(() => {
    // Validation/API failures are already surfaced inside processRowUpdate
    // (notifyWarning / the global error handler) — MUI just requires this
    // handler to exist so the rejection isn't logged as an uncaught error.
  }, []);

  const draftRowActions = useMemo<TableRowAction<PlayerRow>[]>(
    () => [
      {
        label: 'Guardar',
        color: 'primary',
        icon: <CheckIcon fontSize="small" />,
        onClick: row => handleSaveDraftRow(row.id),
      },
      {
        label: 'Descartar',
        color: 'error',
        icon: <CloseIcon fontSize="small" />,
        onClick: row => handleDiscardDraftRow(row.id),
      },
    ],
    [handleSaveDraftRow, handleDiscardDraftRow]
  );

  const isDraftCellEditable = useCallback(
    (params: GridCellParams<PlayerRow>) => Boolean(params.row.isNew),
    []
  );

  const columns: GridColDef<PlayerRow>[] = useMemo(() => {
    // Omitted entirely (not just read-only) when scoped to one team's own
    // roster page — every row would show the exact same value, which is
    // noise, not information. Only the global players list (no `teamId`)
    // needs it, to say WHICH team each row belongs to.
    const teamColumn: GridColDef<PlayerRow> | null = teamId
      ? null
      : {
          field: 'teamId',
          headerName: 'Equipo',
          flex: 1,
          minWidth: 160,
          editable: true,
          type: 'singleSelect',
          valueOptions: teamOptions.map(team => ({
            value: team.id,
            label: team.name,
          })),
          renderCell: params => teamNameById.get(params.row.teamId) ?? '—',
        };

    const baseColumns: GridColDef<PlayerRow>[] = [
      {
        field: 'firstName',
        headerName: 'Nombre',
        flex: 0.9,
        minWidth: 140,
        editable: true,
      },
      {
        field: 'secondName',
        headerName: 'Segundo nombre',
        flex: 0.9,
        minWidth: 140,
        editable: true,
      },
      {
        field: 'lastName',
        headerName: 'Apellido',
        flex: 0.9,
        minWidth: 140,
        editable: true,
      },
      {
        field: 'documentNumber',
        headerName: 'Documento',
        flex: 0.8,
        minWidth: 140,
        align: 'center',
        headerAlign: 'center',
        editable: true,
        renderCell: params =>
          params.row.documentNumber
            ? formatDocumentNumber(params.row.documentNumber)
            : '—',
      },
      {
        field: 'birthDate',
        headerName: 'Fecha de nacimiento',
        flex: 0.9,
        minWidth: 170,
        editable: true,
        renderEditCell: params => <BirthDateEditCell {...params} />,
        renderCell: params => {
          const value = params.row.isNew
            ? rowBirthDateValue(params.row)
            : params.row.birthDate;
          if (!value) {
            return '—';
          }
          const date = new Date(value);
          return Number.isNaN(date.getTime())
            ? '—'
            : date.toLocaleDateString('es-AR');
        },
      },
      ...(teamColumn ? [teamColumn] : []),
      {
        field: 'phoneNumber',
        headerName: 'Teléfono',
        flex: 0.9,
        minWidth: 140,
        editable: true,
        renderCell: params =>
          params.row.phoneNumber
            ? formatArgentinePhone(params.row.phoneNumber)
            : '—',
      },
      {
        field: 'socialSecurity',
        headerName: 'Obra social',
        flex: 1,
        minWidth: 160,
        editable: true,
        renderCell: params => params.row.socialSecurity || '—',
      },
    ];

    if (rosterEnabled) {
      baseColumns.push({
        field: 'jerseyNumber',
        headerName: 'Dorsal',
        flex: 0.5,
        minWidth: 100,
        sortable: false,
        filterable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => {
          const dorsal = currentDorsalFor(params.row);
          return dorsal === null || dorsal === undefined ? '—' : dorsal;
        },
      });
    }

    if (medicalEnabled) {
      baseColumns.push({
        field: 'habilitacion',
        headerName: 'Habilitación',
        flex: 0.9,
        minWidth: 150,
        sortable: false,
        filterable: false,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => {
          const info = medicalByPlayerId?.get(params.row.id);
          return (
            <HabilitacionBadge
              isHabilitado={info?.isHabilitado ?? params.row.isHabilitado}
              status={info?.status ?? params.row.medicalRecordStatus}
            />
          );
        },
      });
    }

    // The actions column's width must grow with how many icon buttons are
    // actually visible (Ver/Eliminar always, plus Ficha médica/Dorsal in
    // roster context) — a fixed width clips the later ones instead of
    // rendering them, so they're invisible and unclickable rather than
    // absent (HU: "no puedo editar o borrar jugadores desde el plantel").
    const visibleActionCount = playerActions.filter(
      action => action.hidden !== true
    ).length;

    baseColumns.push({
      field: 'actions',
      headerName: 'Acciones',
      sortable: false,
      filterable: false,
      align: 'center',
      headerAlign: 'center',
      minWidth: 40 * visibleActionCount + 40,
      renderCell: params =>
        params.row.isNew ? (
          <TableRowActions row={params.row} actions={draftRowActions} />
        ) : (
          <TableRowActions row={params.row} actions={playerActions} />
        ),
    });

    return baseColumns;
  }, [
    currentDorsalFor,
    draftRowActions,
    medicalByPlayerId,
    medicalEnabled,
    playerActions,
    rosterEnabled,
    teamId,
    teamNameById,
    teamOptions,
  ]);

  const rows = useMemo(
    () => [...draftRows, ...(players ?? [])],
    [draftRows, players]
  );

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

  const handleDorsalSubmit = async () => {
    if (!dorsalPlayer || !teamId || !tournamentId) {
      return;
    }

    const parsedDorsal = parseDorsalValue(dorsalValue);
    if (!parsedDorsal.success) {
      void notifyWarning({
        title: 'Dorsal inválido',
        text: 'El dorsal debe ser un número entero entre 0 y 99.',
      });
      return;
    }
    const jerseyNumber = parsedDorsal.jerseyNumber;

    setSubmitting(true);
    const result = await registerPlayerToTeam(dorsalPlayer.id, {
      teamId,
      tournamentId,
      jerseyNumber,
    });
    setSubmitting(false);

    if (!result.success) {
      // HU-54: surface the exact roster conflict (duplicate dorsal / roster
      // full / player already in another team of this tournament).
      await notifyError({
        title: 'No se pudo asignar el dorsal',
        text: result.errorMessage,
      });
      return;
    }

    setDorsalPlayer(null);
    setDorsalValue('');
    onMedicalChange?.();
    await fetchPlayers(debouncedFilters, paginationModel);
    await notifySuccess({
      title: 'Dorsal actualizado',
      text:
        jerseyNumber === null
          ? 'Se quitó el dorsal del jugador.'
          : `Se asignó el dorsal ${jerseyNumber}.`,
    });
  };

  const createButton = createType ? (
    <NewEntityButton type={createType} onClick={handleCreatePlayer} />
  ) : null;

  const body = (
    <>
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de jugadores">
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
      </FilterBar>

      <TableScrollBox>
        <DataGrid
          rows={rows}
          columns={columns}
          loading={loading}
          getRowId={row => row.id}
          autoHeight
          disableRowSelectionOnClick
          disableColumnMenu
          editMode="row"
          isCellEditable={isDraftCellEditable}
          rowModesModel={rowModesModel}
          onRowModesModelChange={setRowModesModel}
          processRowUpdate={processRowUpdate}
          onProcessRowUpdateError={handleProcessRowUpdateError}
          localeText={dataGridLocaleText(noRowsMessage)}
          pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
          paginationModel={paginationModel}
          onPaginationModelChange={handlePaginationModelChange}
          paginationMode="server"
          rowCount={rowCount}
        />
      </TableScrollBox>

      <Dialog
        open={Boolean(dorsalPlayer)}
        onClose={() => !submitting && setDorsalPlayer(null)}
        fullWidth
        maxWidth="xs"
      >
        <DialogTitle>Asignar dorsal</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {dorsalPlayer && (
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                {dorsalPlayer.fullName}
              </Typography>
            )}
            <TextField
              label="Dorsal"
              type="number"
              value={dorsalValue}
              onChange={e => setDorsalValue(e.target.value)}
              fullWidth
              slotProps={{
                htmlInput: { min: 0, max: 99, step: 1 },
                input: {
                  endAdornment: (
                    <FieldInfoTooltip title="Único por equipo y temporada. Dejar vacío para quitarlo." />
                  ),
                },
              }}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={() => {
              setDorsalPlayer(null);
              setDorsalValue('');
            }}
            onConfirm={() => void handleDorsalSubmit()}
            confirmLabel="Guardar"
            disabled={submitting}
          />
        </DialogActions>
      </Dialog>

      {medicalEnabled && teamId && tournamentId && medicalPlayer && (
        <PlayerMedicalRecordDialog
          open={Boolean(medicalPlayer)}
          onClose={() => setMedicalPlayer(null)}
          playerId={medicalPlayer.id}
          teamId={teamId}
          tournamentId={tournamentId}
          playerName={medicalPlayer.fullName}
          status={
            medicalByPlayerId?.get(medicalPlayer.id)?.status ??
            medicalPlayer.medicalRecordStatus
          }
          isHabilitado={
            medicalByPlayerId?.get(medicalPlayer.id)?.isHabilitado ??
            medicalPlayer.isHabilitado
          }
          onChanged={() => {
            onMedicalChange?.();
            void fetchPlayers(debouncedFilters, paginationModel);
          }}
        />
      )}
    </>
  );

  if (wrapInCard) {
    return (
      <PageShell title={title} actions={createButton}>
        {body}
      </PageShell>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      {createButton && (
        <Stack
          direction="row"
          sx={{ justifyContent: 'flex-end', mb: 2 }}
        >
          {createButton}
        </Stack>
      )}
      {body}
    </Box>
  );
};

export default PlayersPage;
