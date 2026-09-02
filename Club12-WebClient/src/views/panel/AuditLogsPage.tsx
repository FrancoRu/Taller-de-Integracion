import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import {
  Box,
  Chip,
  FormControl,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';
import { useAuditLog } from '@/modules/auditLog/hook/auditLog.hook';
import {
  AuditAction,
  AuditLogFiltered,
  IAuditLogResponse,
} from '@/modules/auditLog/type/auditLog';
import { SearchIcon } from '@/views/core/MUI/icons/icons';
import PageShell from '@/views/core/components/PageShell';
import FilterBar from '@/views/core/components/FilterBar';
import {
  TABLE_PAGE_SIZE_OPTIONS,
  TABLE_ROWS_PER_PAGE,
} from '@/modules/core/constants/pagination';
import { FILTERS_DEBOUNCE_DELAY_LONG_MS } from '@/modules/core/constants/constants';

/** Spanish labels for the auditable action types (HU-101). */
const ACTION_LABELS: Record<AuditAction, string> = {
  DataWipe: 'Borrado total de datos',
  BackupRestore: 'Restauración de respaldo',
  TournamentStatusChange: 'Cambio de estado de torneo',
  PasswordReset: 'Blanqueo de contraseña',
};

const ACTION_OPTIONS = Object.keys(ACTION_LABELS) as AuditAction[];

const actionLabel = (action: string): string =>
  ACTION_LABELS[action as AuditAction] ?? action;

/**
 * The "Objetivo" cell text: the target's captured name/label when the entry
 * has one, else the raw type+id (older entries, written before targetName
 * existed, or actions with no single named target).
 */
const targetLabel = (row: IAuditLogResponse): string => {
  const { targetType, targetId, targetName } = row;
  if (!targetType && !targetId) {
    return '—';
  }
  if (targetName) {
    return targetType ? `${targetType}: ${targetName}` : targetName;
  }
  return [targetType, targetId].filter(Boolean).join(': ');
};

/** Truncates a DataGrid cell's text with an ellipsis, showing the full value in a tooltip on hover. */
const TruncatedCell = ({ value }: { value: string }) => (
  <Tooltip title={value}>
    <Box
      sx={{
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
      }}
    >
      {value}
    </Box>
  </Tooltip>
);

interface AuditFilters {
  actor?: string;
  action?: AuditAction | '';
}

const EMPTY_FILTERS: AuditFilters = { actor: '', action: '' };

/**
 * Admin/Owner-only view of the sensitive-action audit trail (HU-101): a table
 * of who did what, to which target, when, and with what context.
 */
const AuditLogsPage: React.FC = () => {
  const { getAuditLogs } = useAuditLog();
  const [rows, setRows] = useState<IAuditLogResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [rowCount, setRowCount] = useState(0);
  const [filters, setFilters] = useState<AuditFilters>(EMPTY_FILTERS);
  const [debouncedFilters, setDebouncedFilters] =
    useState<AuditFilters>(EMPTY_FILTERS);
  const [paginationModel, setPaginationModel] = useState<GridPaginationModel>({
    page: 0,
    pageSize: TABLE_ROWS_PER_PAGE,
  });

  const getAuditLogsRef = useRef(getAuditLogs);
  useEffect(() => {
    getAuditLogsRef.current = getAuditLogs;
  }, [getAuditLogs]);

  const fetchLogs = useCallback(
    async (
      activeFilters: AuditFilters,
      activePaginationModel: GridPaginationModel
    ) => {
      setLoading(true);
      const request: AuditLogFiltered = {
        pageNumber: activePaginationModel.page + 1,
        pageSize: activePaginationModel.pageSize,
        actor: activeFilters.actor || undefined,
        action: activeFilters.action || undefined,
      };
      const response = await getAuditLogsRef.current(request);
      if (response) {
        setRows(response.items);
        setRowCount(response.totalCount);
      }
      setLoading(false);
    },
    []
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      setDebouncedFilters(filters);
    }, FILTERS_DEBOUNCE_DELAY_LONG_MS);

    return () => clearTimeout(timeoutId);
  }, [filters]);

  useEffect(() => {
    void fetchLogs(debouncedFilters, paginationModel);
  }, [debouncedFilters, fetchLogs, paginationModel]);

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

  const handleClearFilters = () => {
    setFilters(EMPTY_FILTERS);
    setDebouncedFilters(EMPTY_FILTERS);
  };

  const columns: GridColDef<IAuditLogResponse>[] = useMemo(
    () => [
      {
        field: 'timestamp',
        headerName: 'Fecha y hora',
        flex: 1,
        minWidth: 170,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => formatDateTimeAr(params.row.timestamp),
      },
      {
        field: 'actor',
        headerName: 'Responsable',
        flex: 1,
        minWidth: 180,
      },
      {
        field: 'action',
        headerName: 'Acción',
        flex: 1.1,
        minWidth: 200,
        align: 'center',
        headerAlign: 'center',
        renderCell: params => (
          <Chip size="small" label={actionLabel(params.row.action)} />
        ),
      },
      {
        field: 'target',
        headerName: 'Objetivo',
        flex: 1,
        minWidth: 180,
        sortable: false,
        filterable: false,
        renderCell: params => <TruncatedCell value={targetLabel(params.row)} />,
      },
      {
        field: 'detail',
        headerName: 'Detalle',
        flex: 1.6,
        minWidth: 240,
        sortable: false,
        filterable: false,
        renderCell: params => <TruncatedCell value={params.row.detail || '—'} />,
      },
    ],
    []
  );

  return (
    <PageShell title="Registro de auditoría">
      <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
        Trazabilidad de acciones sensibles: borrados totales, restauraciones,
        cambios de estado de torneo y blanqueos de contraseña.
      </Typography>

      <FilterBar onClear={handleClearFilters} ariaLabel="Filtros de auditoría">
        <TextField
          label="Responsable"
          name="actor"
          size="small"
          value={filters.actor ?? ''}
          onChange={e =>
            setFilters(prev => ({ ...prev, actor: e.target.value }))
          }
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
        <FormControl size="small" sx={{ minWidth: 220 }}>
          <InputLabel id="audit-action-filter-label">Acción</InputLabel>
          <Select
            labelId="audit-action-filter-label"
            label="Acción"
            value={filters.action ?? ''}
            onChange={e =>
              setFilters(prev => ({
                ...prev,
                action: e.target.value as AuditAction | '',
              }))
            }
          >
            <MenuItem value="">Todas</MenuItem>
            {ACTION_OPTIONS.map(action => (
              <MenuItem key={action} value={action}>
                {ACTION_LABELS[action]}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
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
          localeText={dataGridLocaleText('No hay acciones registradas.')}
          pageSizeOptions={TABLE_PAGE_SIZE_OPTIONS}
          paginationModel={paginationModel}
          onPaginationModelChange={handlePaginationModelChange}
          paginationMode="server"
          rowCount={rowCount}
        />
      </Box>
    </PageShell>
  );
};

export default AuditLogsPage;
