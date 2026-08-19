import { useState } from 'react';
import { Box, Button, Card, CardContent, Stack, Typography } from '@mui/material';
import { isAxiosError } from 'axios';
import ScienceIcon from '@mui/icons-material/Science';
import DeleteSweepIcon from '@mui/icons-material/DeleteSweep';
import { dataMaintenanceService } from '@/modules/dataMaintenance/service/dataMaintenance.service';
import {
  confirmDelete,
  notifyError,
  notifySuccess,
} from '@/modules/core/utils/confirmDialog';

const TestDataPage: React.FC = () => {
  const [isWiping, setIsWiping] = useState(false);
  const [isSeeding, setIsSeeding] = useState(false);

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
    }
  };

  const handleSeed = async (): Promise<void> => {
    setIsSeeding(true);
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
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      <Typography variant="h5" sx={{ mb: 2 }}>
        Herramientas de datos de prueba
      </Typography>
      <Card>
        <CardContent>
          <Typography variant="body2" sx={{ mb: 3 }}>
            Estas herramientas afectan solo torneos, equipos, jugadores, partidos,
            sanciones y estadísticas. Los usuarios y roles nunca se tocan.
          </Typography>
          <Stack direction="row" spacing={2}>
            <Button
              variant="outlined"
              color="error"
              startIcon={<DeleteSweepIcon />}
              disabled={isWiping || isSeeding}
              onClick={handleWipe}
            >
              Borrar DB
            </Button>
            <Button
              variant="contained"
              startIcon={<ScienceIcon />}
              disabled={isWiping || isSeeding}
              onClick={handleSeed}
            >
              Cargar Datos de prueba
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Box>
  );
};

export default TestDataPage;
