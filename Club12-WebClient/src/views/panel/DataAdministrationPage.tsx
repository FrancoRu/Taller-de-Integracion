import { useEffect, useState } from 'react';
import {
  Button,
  Card,
  CardContent,
  CircularProgress,
  Dialog,
  DialogContent,
  Stack,
  Typography,
} from '@mui/material';
import { BackupIcon, DeleteSweepIcon } from '@/views/core/MUI/icons/icons';
import { dataMaintenanceService } from '@/modules/dataMaintenance/service/dataMaintenance.service';
import { useBackups } from '@/modules/backup/hook/backup.hook';
import BackupsTable from '@/views/panel/components/BackupsTable';
import PageShell from '@/views/core/components/PageShell';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';

const DataAdministrationPage: React.FC = () => {
  const [isWiping, setIsWiping] = useState(false);
  const [activeOperation, setActiveOperation] = useState<string | null>(null);
  const {
    backups,
    loading,
    busy,
    fetchBackups,
    createBackup,
    deleteBackup,
    restoreBackup,
  } = useBackups();

  useEffect(() => {
    void fetchBackups();
  }, [fetchBackups]);

  // IMPORTANT: the blocking overlay (a MUI Dialog, z-index 1300) sits ABOVE the
  // SweetAlert toasts (z-index ~1060). So the overlay MUST be closed BEFORE any
  // notify* call — otherwise the toast renders behind the overlay backdrop, its
  // "OK" is unclickable, the awaited promise never resolves, and the panel
  // freezes. Every handler clears activeOperation first, then notifies.
  const handleWipe = async (): Promise<void> => {
    const confirmed = await confirmDelete({
      title: '¿Borrar todos los datos de prueba?',
      text: 'Se eliminan todos los torneos, equipos, jugadores, partidos, sanciones y estadísticas. Los usuarios no se ven afectados. Esta acción no se puede deshacer.',
      confirmButtonText: 'Sí, borrar todo',
    });

    if (!confirmed) {
      return;
    }

    setIsWiping(true);
    setActiveOperation('Borrando todos los datos de prueba…');
    let response: Awaited<
      ReturnType<typeof dataMaintenanceService.wipeSampleData>
    > | null = null;
    let failed = false;
    try {
      response = await dataMaintenanceService.wipeSampleData();
    } catch {
      failed = true;
    } finally {
      setIsWiping(false);
      setActiveOperation(null);
    }

    if (failed || !response) {
      await notifyError({
        title: 'No se pudo borrar la base de datos',
        text: 'Volvé a intentar en unos segundos.',
      });
      return;
    }

    await notifySuccess({
      title: 'Base de datos vaciada',
      text: `${response.data.tournaments} torneos, ${response.data.teams} equipos y ${response.data.players} jugadores eliminados.`,
    });
  };

  const handleGenerateBackup = async (): Promise<void> => {
    setActiveOperation('Generando el respaldo de la base de datos…');
    let created: boolean;
    try {
      created = await createBackup();
    } finally {
      setActiveOperation(null);
    }

    if (!created) {
      await notifyError({
        title: 'No se pudo generar el respaldo',
        text: 'Puede haber otra operación de respaldo/restauración en curso. Volvé a intentar en unos segundos.',
      });
      return;
    }

    await notifySuccess({ title: 'Respaldo generado' });
  };

  const handleDeleteBackup = async (backup: {
    id: string;
  }): Promise<void> => {
    setActiveOperation('Eliminando el respaldo…');
    let deleted: boolean;
    try {
      deleted = await deleteBackup(backup.id);
    } finally {
      setActiveOperation(null);
    }

    if (!deleted) {
      await notifyError({ title: 'No se pudo eliminar el respaldo' });
      return;
    }

    await notifySuccess({ title: 'Respaldo eliminado' });
  };

  const handleRestoreBackup = async (backup: {
    id: string;
  }): Promise<void> => {
    setActiveOperation(
      'Restaurando la base de datos desde el respaldo. No cierres ni recargues esta página…'
    );
    let restored: boolean;
    try {
      restored = await restoreBackup(backup.id);
    } finally {
      setActiveOperation(null);
    }

    if (!restored) {
      await notifyError({ title: 'No se pudo restaurar el respaldo' });
      return;
    }

    await notifySuccess({ title: 'Base de datos restaurada' });
  };

  return (
    <PageShell title="Administración de datos">
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Base de datos
          </Typography>
          <Stack direction="row" spacing={2} sx={{ mb: 3 }}>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteSweepIcon />}
              disabled={isWiping}
              onClick={handleWipe}
            >
              Borrar los datos
            </Button>
            <Button
              variant="contained"
              startIcon={<BackupIcon />}
              disabled={busy}
              onClick={() => void handleGenerateBackup()}
            >
              Generar respaldo
            </Button>
          </Stack>
          <BackupsTable
            backups={backups}
            loading={loading}
            onDelete={handleDeleteBackup}
            onRestore={handleRestoreBackup}
          />
        </CardContent>
      </Card>

      {/* Blocking overlay: while a destructive/long data operation runs, cover
          the panel so no other action (or navigation via the controls behind
          it) can start until it finishes. */}
      <Dialog
        open={Boolean(activeOperation)}
        aria-labelledby="data-admin-operation-title"
      >
        <DialogContent>
          <Stack spacing={2} sx={{ alignItems: 'center', py: 2, px: 3 }}>
            <CircularProgress />
            <Typography
              id="data-admin-operation-title"
              variant="subtitle1"
              sx={{ textAlign: 'center' }}
            >
              {activeOperation}
            </Typography>
            <Typography
              variant="body2"
              sx={{ color: 'text.secondary', textAlign: 'center' }}
            >
              La operación puede tardar unos segundos. Esperá a que termine sin
              cerrar esta ventana.
            </Typography>
          </Stack>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
};

export default DataAdministrationPage;
