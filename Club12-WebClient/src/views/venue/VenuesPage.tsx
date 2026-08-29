import { useCallback, useEffect, useMemo, useState } from 'react';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import {
  Avatar,
  Box,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  InputAdornment,
  Stack,
  TextField,
  Typography,
  Button,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import {
  IAddVenueRequest,
  IPutVenueRequest,
  IVenueResponse,
} from '@/modules/venue/type/venue';
import { useVenue } from '@/modules/venue/hook/venue.hook';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import FormButtons from '@/views/core/components/FormButtons';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import {
  DeleteIcon,
  EditIcon,
  SearchIcon,
  VisibilityIcon,
} from '@/views/core/MUI/icons/icons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';

interface VenuesPageProps {
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
}

type VenueSearchFilters = {
  name?: string;
  address?: string;
};

type VenueFormState = {
  name: string;
  address: string;
  imageFile: File | null;
};

const EMPTY_FILTERS: VenueSearchFilters = {};

const INITIAL_VENUE_FORM: VenueFormState = {
  name: '',
  address: '',
  imageFile: null,
};

const VenuesPage: React.FC<VenuesPageProps> = ({
  emptyMessage = 'No hay canchas cargadas.',
  title = 'Canchas',
  wrapInCard = true,
}) => {
  const navigate = useNavigate();
  const { venues, addVenue, putVenueById, deleteVenueById, getAllVenues } =
    useVenue();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [filters, setFilters] = useState<VenueSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<VenueSearchFilters>(EMPTY_FILTERS);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingVenue, setEditingVenue] = useState<IVenueResponse | null>(null);
  const [venueForm, setVenueForm] =
    useState<VenueFormState>(INITIAL_VENUE_FORM);

  const fetchVenues = useCallback(async () => {
    setLoading(true);
    await getAllVenues();
    setLoading(false);
  }, [getAllVenues]);

  useEffect(() => {
    void fetchVenues();
  }, [fetchVenues]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  const resetVenueForm = useCallback(() => {
    setVenueForm(INITIAL_VENUE_FORM);
  }, []);

  const handleFilterChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = e.target;
    setFilters(prev => ({
      ...prev,
      [name]: value || undefined,
    }));
  };

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
  };

  const rows = useMemo(() => venues ?? [], [venues]);

  const filteredRows = useMemo(() => {
    const nameFilter = debouncedFilters.name?.trim().toLowerCase();
    const addressFilter = debouncedFilters.address?.trim().toLowerCase();

    return rows.filter(row => {
      const byName = !nameFilter || row.name.toLowerCase().includes(nameFilter);
      const byAddress =
        !addressFilter || row.address.toLowerCase().includes(addressFilter);
      return byName && byAddress;
    });
  }, [rows, debouncedFilters.address, debouncedFilters.name]);

  const hasActiveFilters = useMemo(
    () => Boolean(filters.name) || Boolean(filters.address),
    [filters.name, filters.address]
  );

  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron canchas para el filtro aplicado.'
    : emptyMessage;

  const handleView = useCallback(
    (row: IVenueResponse) => {
      navigate(APP_ROUTES.panelVenue.build(row.slug));
    },
    [navigate]
  );

  const handleEdit = useCallback((row: IVenueResponse) => {
    setEditingVenue(row);
    setVenueForm({
      name: row.name,
      address: row.address,
      imageFile: null,
    });
  }, []);

  const handleDelete = useCallback(
    async (row: IVenueResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar esta cancha?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      const result = await deleteVenueById(row.id);

      if (!result.success) {
        // Surface the backend integrity block (the venue is referenced by
        // matches) with its exact Spanish message.
        await notifyError({
          title: 'No se pudo eliminar',
          text: result.errorMessage,
        });
        return;
      }

      await notifySuccess({
        title: '¡Eliminada!',
        text: 'La cancha ha sido eliminada.',
      });

      await fetchVenues();
    },
    [deleteVenueById, fetchVenues]
  );

  const venueActions = useMemo<TableRowAction<IVenueResponse>[]>(
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

  const columns: GridColDef<IVenueResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<IVenueResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1,
        minWidth: 200,
        renderCell: params => (
          <Stack
            direction="row"
            spacing={1}
            sx={{ alignItems: 'center', height: '100%' }}
          >
            {/* A venue is a court, not a team — only show an avatar when it has
                a real photo; no initials/icon placeholder otherwise. */}
            {params.row.photoUrl && (
              <Avatar
                src={params.row.photoUrl}
                variant="rounded"
                sx={{ width: 28, height: 28 }}
              />
            )}
            <Typography variant="body2">{params.row.name}</Typography>
          </Stack>
        ),
      },
      {
        field: 'address',
        headerName: 'Dirección',
        flex: 1.3,
        minWidth: 240,
      },
    ];

    return [
      ...baseColumns,
      buildActionsColumn(venueActions, {
        align: 'center',
        headerAlign: 'center',
      }),
    ];
  }, [venueActions]);

  const handleCreateSubmit = async () => {
    if (!venueForm.name.trim() || !venueForm.address.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y dirección son obligatorios.',
      });
      return;
    }

    if (!venueForm.imageFile) {
      await notifyWarning({
        title: 'Imagen requerida',
        text: 'Debe seleccionar una imagen para crear la cancha.',
      });
      return;
    }

    setSubmitting(true);
    const payload: IAddVenueRequest = {
      name: venueForm.name.trim(),
      address: venueForm.address.trim(),
      imageFile: venueForm.imageFile,
    };

    const created = await addVenue(payload);
    setSubmitting(false);

    if (!created) {
      return;
    }

    setIsCreateModalOpen(false);
    resetVenueForm();
    await fetchVenues();
  };

  const handleEditSubmit = async () => {
    if (!editingVenue) {
      return;
    }

    if (!venueForm.name.trim() || !venueForm.address.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y dirección son obligatorios.',
      });
      return;
    }

    setSubmitting(true);
    const payload: IPutVenueRequest = {
      name: venueForm.name.trim(),
      address: venueForm.address.trim(),
    };

    const updated = await putVenueById(editingVenue.id, payload);
    setSubmitting(false);

    if (updated === undefined) {
      return;
    }

    setEditingVenue(null);
    resetVenueForm();
    await fetchVenues();
  };

  const createButton = (
    <NewEntityButton
      type="Cancha"
      gender="feminine"
      onClick={() => {
        resetVenueForm();
        setIsCreateModalOpen(true);
      }}
    />
  );

  const body = (
    <>
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de canchas">
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

        <TextField
          label="Dirección"
          name="address"
          size="small"
          value={filters.address ?? ''}
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

      <Box sx={{ width: '100%' }}>
        <DataGrid
          rows={filteredRows}
          columns={columns}
          loading={loading}
          getRowId={row => row.id}
          autoHeight
          disableRowSelectionOnClick
          disableColumnMenu
          localeText={dataGridLocaleText(noRowsMessage)}
          pageSizeOptions={[...TABLE_PAGE_SIZE_OPTIONS]}
          initialState={{
            pagination: { paginationModel: { pageSize: TABLE_ROWS_PER_PAGE } },
          }}
        />
      </Box>

      <Dialog
        open={isCreateModalOpen}
        onClose={() => !submitting && setIsCreateModalOpen(false)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Nueva cancha</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Nombre"
              value={venueForm.name}
              onChange={e =>
                setVenueForm(prev => ({ ...prev, name: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Dirección"
              value={venueForm.address}
              onChange={e =>
                setVenueForm(prev => ({ ...prev, address: e.target.value }))
              }
              required
              fullWidth
            />
            <Button variant="outlined" component="label">
              {venueForm.imageFile
                ? `Imagen: ${venueForm.imageFile.name}`
                : 'Seleccionar imagen'}
              <input
                type="file"
                accept="image/*"
                hidden
                onChange={e => {
                  const file = e.target.files?.[0] ?? null;
                  setVenueForm(prev => ({ ...prev, imageFile: file }));
                }}
              />
            </Button>
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={() => {
              setIsCreateModalOpen(false);
              resetVenueForm();
            }}
            onConfirm={() => void handleCreateSubmit()}
            confirmLabel="Crear"
            disabled={submitting}
          />
        </DialogActions>
      </Dialog>

      <Dialog
        open={Boolean(editingVenue)}
        onClose={() => !submitting && setEditingVenue(null)}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>Editar cancha</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Nombre"
              value={venueForm.name}
              onChange={e =>
                setVenueForm(prev => ({ ...prev, name: e.target.value }))
              }
              required
              fullWidth
            />
            <TextField
              label="Dirección"
              value={venueForm.address}
              onChange={e =>
                setVenueForm(prev => ({ ...prev, address: e.target.value }))
              }
              required
              fullWidth
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <FormButtons
            onCancel={() => {
              setEditingVenue(null);
              resetVenueForm();
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
      <PageShell title={title} actions={createButton}>
        {body}
      </PageShell>
    );
  }

  return (
    <Box sx={{ width: '100%' }}>
      <Stack direction="row" sx={{ justifyContent: 'flex-end', mb: 2 }}>
        {createButton}
      </Stack>
      {body}
    </Box>
  );
};

export default VenuesPage;
