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
import { isAxiosError } from 'axios';
import {
  BackupIcon,
  DeleteSweepIcon,
  ScienceIcon,
} from '@/views/core/MUI/icons/icons';
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
  const [isSeeding, setIsSeeding] = useState(false);
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
    try {
      const response = await dataMaintenanceService.wipeSampleData();
      await notifySuccess({
        title: 'Base de datos vaciada',
        text: `${response.data.tournaments} torneos, ${response.data.teams} equipos y ${response.data.players} jugadores eliminados.`,
      });
    } catch {
      await notifyError({
        title: 'No se pudo borrar la base de datos',
        text: 'Volvé a intentar en unos segundos.',
      });
    } finally {
      setIsWiping(false);
      setActiveOperation(null);
    }
  };

  const handleSeed = async (): Promise<void> => {
    setIsSeeding(true);
    setActiveOperation('Cargando datos de prueba…');
    try {
      const response = await dataMaintenanceService.seedSampleData();
      await notifySuccess({
        title: 'Datos de prueba cargados',
        text: `${response.data.tournaments} torneos, ${response.data.teams} equipos y ${response.data.players} jugadores creados.`,
      });
    } catch (error) {
      const isConflict = isAxiosError(error) && error.response?.status === 409;
      await notifyError({
        title: 'No se pudieron cargar los datos de prueba',
        text: isConflict
          ? 'La base ya tiene datos. Borrala primero con "Borrar DB".'
          : 'Volvé a intentar en unos segundos.',
      });
    } finally {
      setIsSeeding(false);
      setActiveOperation(null);
    }
  };

  const handleGenerateBackup = async (): Promise<void> => {
    setActiveOperation('Generando el respaldo de la base de datos…');
    try {
      const created = await createBackup();
      if (!created) {
        await notifyError({
          title: 'No se pudo generar el respaldo',
          text: 'Puede haber otra operación de respaldo/restauración en curso. Volvé a intentar en unos segundos.',
        });
        return;
      }

      await notifySuccess({ title: 'Respaldo generado' });
    } finally {
      setActiveOperation(null);
    }
  };

  const handleDeleteBackup = async (backup: {
    id: string;
  }): Promise<void> => {
    setActiveOperation('Eliminando el respaldo…');
    try {
      const deleted = await deleteBackup(backup.id);
      if (!deleted) {
        await notifyError({ title: 'No se pudo eliminar el respaldo' });
        return;
      }

      await notifySuccess({ title: 'Respaldo eliminado' });
    } finally {
      setActiveOperation(null);
    }
  };

  const handleRestoreBackup = async (backup: {
    id: string;
  }): Promise<void> => {
    setActiveOperation(
      'Restaurando la base de datos desde el respaldo. No cierres ni recargues esta página…'
    );
    try {
      const restored = await restoreBackup(backup.id);
      if (!restored) {
        await notifyError({ title: 'No se pudo restaurar el respaldo' });
        return;
      }

      await notifySuccess({ title: 'Base de datos restaurada' });
    } finally {
      setActiveOperation(null);
    }
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
              disabled={isWiping || isSeeding}
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

      <Card>
        <CardContent>
          <Typography variant="h6" sx={{ mb: 2 }}>
            Test
          </Typography>
          <Button
            variant="contained"
            startIcon={<ScienceIcon />}
            disabled={isWiping || isSeeding}
            onClick={() => void handleSeed()}
          >
            Cargar Datos de prueba
          </Button>
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
