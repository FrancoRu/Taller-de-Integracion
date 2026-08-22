import { useMemo } from 'react';
import { Box } from '@mui/material';
import { DataGrid, GridColDef } from '@mui/x-data-grid';
import { confirmAction, confirmDelete } from '@/modules/core/utils/confirmDialog';
import {
  BACKUP_ORIGIN_LABELS,
  formatBytes,
} from '@/modules/backup/utils/backupFormat';
import type { IBackupRecordResponse } from '@/modules/backup/type/backup';
import { buildActionsColumn } from '@/views/core/components/buildActionsColumn';
import { TableRowAction } from '@/views/core/components/TableRowActions';
import { DeleteIcon, RestoreIcon } from '@/views/core/MUI/icons/icons';

interface BackupsTableProps {
  backups: IBackupRecordResponse[];
  loading: boolean;
  onDelete: (backup: IBackupRecordResponse) => Promise<void> | void;
  onRestore: (backup: IBackupRecordResponse) => Promise<void> | void;
}

const BackupsTable: React.FC<BackupsTableProps> = ({
  backups,
  loading,
  onDelete,
  onRestore,
}) => {
  const handleDelete = async (backup: IBackupRecordResponse): Promise<void> => {
    const confirmed = await confirmDelete({
      title: '¿Eliminar este respaldo?',
      text: 'Esta acción no se puede deshacer.',
    });

    if (!confirmed) {
      return;
    }

    await onDelete(backup);
  };

  const handleRestore = async (backup: IBackupRecordResponse): Promise<void> => {
    const fecha = new Date(backup.createdAt).toLocaleString('es-AR');
    const confirmed = await confirmAction({
      icon: 'warning',
      title: '¿Restaurar la base desde este respaldo?',
      text: `Se sobrescribe TODA la base con el respaldo del ${fecha}. Antes se genera un respaldo automático del estado actual. El sistema queda en mantenimiento durante la operación.`,
      confirmButtonText: 'Sí, restaurar',
    });

    if (!confirmed) {
      return;
    }

    await onRestore(backup);
  };

  const actions = useMemo<TableRowAction<IBackupRecordResponse>[]>(
    () => [
      {
        label: 'Eliminar',
        color: 'error',
        icon: <DeleteIcon fontSize="small" />,
        onClick: backup => void handleDelete(backup),
      },
      {
        label: 'Restaurar',
        color: 'warning',
        icon: <RestoreIcon fontSize="small" />,
        onClick: backup => void handleRestore(backup),
      },
    ],
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [onDelete, onRestore]
  );

  const columns: GridColDef<IBackupRecordResponse>[] = useMemo(
    () => [
      {
        field: 'createdAt',
        headerName: 'Fecha',
        flex: 1,
        minWidth: 180,
        valueFormatter: (value: string) =>
          new Date(value).toLocaleString('es-AR'),
      },
      {
        field: 'sizeBytes',
        headerName: 'Peso',
        flex: 0.6,
        minWidth: 100,
        valueFormatter: (value: number) => formatBytes(value),
      },
      {
        field: 'origin',
        headerName: 'Forma de creación',
        flex: 0.8,
        minWidth: 160,
        valueFormatter: (value: 'Manual' | 'Job') => BACKUP_ORIGIN_LABELS[value],
      },
      buildActionsColumn(actions),
    ],
    [actions]
  );

  return (
    <Box sx={{ width: '100%' }}>
      <DataGrid
        rows={backups}
        columns={columns}
        loading={loading}
        getRowId={row => row.id}
        autoHeight
        disableRowSelectionOnClick
        disableColumnMenu
        localeText={{ noRowsLabel: 'Todavía no hay respaldos generados.' }}
      />
    </Box>
  );
};

export default BackupsTable;
