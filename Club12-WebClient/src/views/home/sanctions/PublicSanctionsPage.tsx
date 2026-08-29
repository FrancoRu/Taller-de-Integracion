import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import {
  Box,
  Chip,
  InputAdornment,
  MenuItem,
  TextField,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionResponse } from '@/modules/playerSanction/type/playerSanction.d';
import {
  formatFechasRemaining,
  formatSanctionDurationFechas,
  getSanctionStateLabel,
  getSanctionSubjectName,
  getSanctionSubjectTypeLabel,
} from '@/modules/playerSanction/utils/sanctionDisplay';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
  FILTER_OPTIONS_PAGE_SIZE,
} from '@/modules/core/constants/pagination';
import { FILTERS_DEBOUNCE_DELAY_MS } from '@/modules/core/constants/constants';

const formatDate = (value?: string | Date | null) => formatDateAr(value);

const columns: GridColDef<IPlayerSanctionResponse>[] = [
  {
    field: 'subject',
    headerName: 'Sujeto',
    flex: 1.2,
    minWidth: 200,
    sortable: false,
    renderCell: params => getSanctionSubjectName(params.row),
  },
  {
    field: 'subjectType',
    headerName: 'Tipo',
    flex: 0.6,
    minWidth: 100,
    renderCell: params => getSanctionSubjectTypeLabel(params.row),
  },
  {
    field: 'duration',
    headerName: 'Duración',
    flex: 0.7,
    minWidth: 120,
    renderCell: params => formatSanctionDurationFechas(params.row.duration),
  },
  {
    field: 'fechasRemaining',
    headerName: 'Fechas restantes',
    flex: 0.7,
    minWidth: 130,
    renderCell: params => formatFechasRemaining(params.row.fechasRemaining),
  },
  {
    field: 'isActive',
    headerName: 'Estado',
    flex: 0.6,
    minWidth: 110,
    renderCell: params => (
      <Chip
        size="small"
        label={getSanctionStateLabel(params.row)}
        color={params.row.isActive ? 'warning' : 'default'}
      />
    ),
  },
  {
    field: 'issuedDate',
    headerName: 'Fecha',
    flex: 0.8,
    minWidth: 120,
    renderCell: params => formatDate(params.row.issuedDate),
  },
  {
    field: 'description',
    headerName: 'Motivo',
    flex: 1.6,
    minWidth: 240,
  },
];

export default function PublicSanctionsPage() {
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>('');
  const [description, setDescription] = useState('');
  const [debouncedDescription, setDebouncedDescription] = useState('');
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });

  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);
  const getPlayerSanctionByFilterRef = useRef(getPlayerSanctionByFilter);

  useEffect(() => {
    getAllTournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    getPlayerSanctionByFilterRef.current = getPlayerSanctionByFilter;
  }, [getPlayerSanctionByFilter]);

  useEffect(() => {
    void getAllTournamentsRef.current({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, []);

  useEffect(() => {
    const timeout = setTimeout(
      () => setDebouncedDescription(description),
      FILTERS_DEBOUNCE_DELAY_MS
    );
    return () => clearTimeout(timeout);
  }, [description]);

  const fetchSanctions = useCallback(
    async (
      tournamentId: GUID | '',
      desc: string,
      pagination: GridPaginationModel
    ) => {
      setLoading(true);
      const response = await getPlayerSanctionByFilterRef.current({
        tournamentId: tournamentId || undefined,
        description: desc || undefined,
        pageNumber: pagination.page + 1,
        pageSize: pagination.pageSize,
      });
      setRowCount(response?.totalCount ?? 0);
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    void fetchSanctions(selectedTournamentId, debouncedDescription, paginationModel);
  }, [fetchSanctions, selectedTournamentId, debouncedDescription, paginationModel]);

  const handleTournamentChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setSelectedTournamentId(e.target.value as GUID | '');
      setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
    },
    []
  );

  const handleDescriptionChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      setDescription(e.target.value);
      setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
    },
    []
  );

  const handlePaginationModelChange = useCallback(
    (next: GridPaginationModel) => {
      setPaginationModel(prev =>
        prev.page === next.page && prev.pageSize === next.pageSize ? prev : next
      );
    },
    []
  );

  const handleClearFilters = useCallback(() => {
    setSelectedTournamentId('');
    setDescription('');
    setPaginationModel(prev => (prev.page === 0 ? prev : { ...prev, page: 0 }));
  }, []);

  const rows = useMemo(() => playerSanctions ?? [], [playerSanctions]);
  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const hasActiveFilters = Boolean(selectedTournamentId || description);

  return (
    <PageShell title="Sanciones">
      <Typography
        variant="body1"
        sx={{
          color: "text.secondary",
          mb: 3
        }}>
        Listado de sanciones aplicadas a jugadores, equipos y staff de la liga.
      </Typography>

      <FilterBar
        ariaLabel="Filtros de sanciones"
        onClear={hasActiveFilters ? handleClearFilters : undefined}
      >
        <TextField
          select
          label="Torneo"
          size="small"
          value={selectedTournamentId}
          onChange={handleTournamentChange}
          sx={{ minWidth: 220 }}
        >
          <MenuItem value="">Todos</MenuItem>
          {tournamentOptions.map(t => (
            <MenuItem key={t.id} value={t.id}>
              {t.name}
            </MenuItem>
          ))}
        </TextField>

        <TextField
          placeholder="Buscar por motivo..."
          size="small"
          value={description}
          onChange={handleDescriptionChange}
          sx={{ minWidth: 260 }}
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
          rows={rows}
          columns={columns}
          loading={loading}
          getRowId={row => row.id}
          autoHeight
          disableRowSelectionOnClick
          disableColumnMenu
          paginationMode="server"
          rowCount={rowCount}
          pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
          paginationModel={paginationModel}
          onPaginationModelChange={handlePaginationModelChange}
          localeText={{
            noRowsLabel: selectedTournamentId || description
              ? 'No se encontraron sanciones para el filtro aplicado.'
              : 'No hay sanciones registradas.',
          }}
        />
      </Box>
    </PageShell>
  );
}
