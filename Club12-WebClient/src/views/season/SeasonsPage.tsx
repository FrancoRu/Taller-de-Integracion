import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { Box, InputAdornment, Stack, TextField } from '@mui/material';
import { notifySuccess, notifyWarning, confirmDelete } from '@/modules/core/utils/confirmDialog';
import { IAddSeasonRequest, ISeasonResponse } from '@/modules/season/type/season';
import { useSeason } from '@/modules/season/hook/season.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import { DeleteIcon, SearchIcon, VisibilityIcon } from '@/views/core/MUI/icons/icons';
import SeasonFormDialog, { SeasonFormState } from '@/views/season/SeasonFormDialog';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';
import {
  FILTER_OPTIONS_PAGE_SIZE,
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';

interface SeasonsPageProps {
  emptyMessage?: string;
  title?: string;
  wrapInCard?: boolean;
}

type SeasonSearchFilters = {
  name?: string;
};

const EMPTY_FILTERS: SeasonSearchFilters = {};

const INITIAL_SEASON_FORM: SeasonFormState = {
  name: '',
  year: '',
};

const SeasonsPage: React.FC<SeasonsPageProps> = ({
  emptyMessage = 'No hay temporadas cargadas.',
  title = 'Temporadas',
  wrapInCard = true,
}) => {
  const { seasons, addSeason, deleteSeasonById, getSeasonsByFiltered } = useSeason();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [filters, setFilters] = useState<SeasonSearchFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<SeasonSearchFilters>(EMPTY_FILTERS);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [seasonForm, setSeasonForm] =
    useState<SeasonFormState>(INITIAL_SEASON_FORM);

  const fetchSeasons = useCallback(async () => {
    setLoading(true);
    await getSeasonsByFiltered({
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
    setLoading(false);
  }, [getSeasonsByFiltered]);

  useEffect(() => {
    void fetchSeasons();
  }, [fetchSeasons]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  const resetSeasonForm = useCallback(() => {
    setSeasonForm(INITIAL_SEASON_FORM);
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

  const rows = useMemo(() => seasons ?? [], [seasons]);

  const filteredRows = useMemo(() => {
    const nameFilter = debouncedFilters.name?.trim().toLowerCase();

    return rows.filter(row => {
      const byName = !nameFilter || row.name.toLowerCase().includes(nameFilter);
      return byName;
    });
  }, [rows, debouncedFilters.name]);

  const hasActiveFilters = useMemo(() => Boolean(filters.name), [filters.name]);

  const noRowsMessage = hasActiveFilters
    ? 'No se encontraron temporadas para el filtro aplicado.'
    : emptyMessage;

  const handleView = useCallback(
    (row: ISeasonResponse) => {
      navigate(APP_ROUTES.panelSeason.build(row.slug ?? row.id));
    },
    [navigate]
  );

  const handleDelete = useCallback(
    async (row: ISeasonResponse) => {
      const confirmed = await confirmDelete({
        title: '¿Está usted seguro de querer eliminar esta temporada?',
        text: '¡Usted no podrá revertir este cambio!',
      });

      if (!confirmed) {
        return;
      }

      await deleteSeasonById(row.id);

      await notifySuccess({
        title: '¡Eliminada!',
        text: 'La temporada ha sido eliminada.',
      });

      await fetchSeasons();
    },
    [deleteSeasonById, fetchSeasons]
  );

  const seasonActions = useMemo<TableRowAction<ISeasonResponse>[]>(
    () => [
      {
        label: 'Ver torneos',
        color: 'primary',
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

  const columns: GridColDef<ISeasonResponse>[] = useMemo(() => {
    const baseColumns: GridColDef<ISeasonResponse>[] = [
      {
        field: 'name',
        headerName: 'Nombre',
        flex: 1,
        minWidth: 200,
      },
      {
        field: 'year',
        headerName: 'Año',
        flex: 0.5,
        minWidth: 120,
        valueGetter: (_value, row) =>
          row.year != null ? String(row.year) : '—',
      },
      {
        field: 'tournaments',
        headerName: 'Torneos',
        flex: 0.5,
        minWidth: 120,
        align: 'center',
        headerAlign: 'center',
        valueGetter: (_value, row) => row.tournaments?.length ?? 0,
      },
    ];

    return [
      ...baseColumns,
      buildActionsColumn(seasonActions, {
        align: 'center',
        headerAlign: 'center',
      }),
    ];
  }, [seasonActions]);

  const parseYear = (value: string): number | null => {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }
    const parsed = Number(trimmed);
    return Number.isFinite(parsed) ? parsed : null;
  };

  const handleCreateSubmit = async () => {
    if (!seasonForm.name.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre es obligatorio.',
      });
      return;
    }

    setSubmitting(true);
    const payload: IAddSeasonRequest = {
      name: seasonForm.name.trim(),
      year: parseYear(seasonForm.year),
    };

    const created = await addSeason(payload);
    setSubmitting(false);

    if (!created) {
      return;
    }

    setIsCreateModalOpen(false);
    resetSeasonForm();
    await fetchSeasons();
  };

  const handleSeasonFieldChange = useCallback(
    (field: keyof SeasonFormState, value: string) => {
      setSeasonForm(prev => ({ ...prev, [field]: value }));
    },
    []
  );

  const createButton = (
    <NewEntityButton
      type="Temporada"
      gender="feminine"
      onClick={() => {
        resetSeasonForm();
        setIsCreateModalOpen(true);
      }}
    />
  );

  const body = (
    <>
      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de temporadas">
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

      <SeasonFormDialog
        open={isCreateModalOpen}
        title="Nueva temporada"
        confirmLabel="Crear"
        form={seasonForm}
        submitting={submitting}
        onFieldChange={handleSeasonFieldChange}
        onClose={() => {
          setIsCreateModalOpen(false);
          resetSeasonForm();
        }}
        onConfirm={() => void handleCreateSubmit()}
      />
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

export default SeasonsPage;
