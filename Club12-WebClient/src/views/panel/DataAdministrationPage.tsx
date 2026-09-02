import { useEffect, useState } from 'react';
import { Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { BackupIcon, DeleteSweepIcon } from '@/views/core/MUI/icons/icons';
import { dataMaintenanceService } from '@/modules/dataMaintenance/service/dataMaintenance.service';
import { useBackups } from '@/modules/backup/hook/backup.hook';
import { runWithBlockingMessage } from '@/modules/core/utils/requestActivity';
import BackupsTable from '@/views/panel/components/BackupsTable';
import PageShell from '@/views/core/components/PageShell';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';

const DataAdministrationPage: React.FC = () => {
  const [isWiping, setIsWiping] = useState(false);
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

  // Each handler runs its work through runWithBlockingMessage: the app-wide
  // GlobalLoadingOverlay already blocks the screen for any mutating request, so
  // this only adds the operation-specific message. SweetAlert's toasts are
  // lifted above that overlay (confirmDialog.liftAboveMuiModals), and the
  // message clears before the notify* call fires anyway.
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
    let response: Awaited<
      ReturnType<typeof dataMaintenanceService.wipeSampleData>
    > | null = null;
    let failed = false;
    try {
      response = await runWithBlockingMessage(
        'Borrando todos los datos de prueba…',
        () => dataMaintenanceService.wipeSampleData()
      );
    } catch {
      failed = true;
    } finally {
      setIsWiping(false);
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
    const created = await runWithBlockingMessage(
      'Generando el respaldo de la base de datos…',
      () => createBackup()
    );

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
    const deleted = await runWithBlockingMessage('Eliminando el respaldo…', () =>
      deleteBackup(backup.id)
    );

    if (!deleted) {
      await notifyError({ title: 'No se pudo eliminar el respaldo' });
      return;
    }

    await notifySuccess({ title: 'Respaldo eliminado' });
  };

  const handleRestoreBackup = async (backup: {
    id: string;
  }): Promise<void> => {
    const restored = await runWithBlockingMessage(
      'Restaurando la base de datos desde el respaldo. No cierres ni recargues esta página…',
      () => restoreBackup(backup.id)
    );

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
    </PageShell>
  );
};

export default DataAdministrationPage;
