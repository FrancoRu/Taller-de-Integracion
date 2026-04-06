import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Grid,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import MatchesPage from '@/views/match/matchesPage';

const formatDate = (value?: string | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleDateString('es-AR');
};

const formatStageType = (value: string) =>
  value.replace(/([a-z])([A-Z])/g, '$1 $2').trim();

const StagePage: React.FC = () => {
  const { stageId } = useParams<{ stageId: GUID }>();
  const navigate = useNavigate();
  const { stage, getStageById } = useStage();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'partidos'>('detalle');

  const targetStageId = useMemo(
    () => stageId ?? stage?.id,
    [stage?.id, stageId]
  );

  useEffect(() => {
    if (!targetStageId) {
      return;
    }

    const fetchStage = async () => {
      setLoading(true);
      await getStageById(targetStageId);
      setLoading(false);
    };

    void fetchStage();
  }, [getStageById, targetStageId]);

  if (!targetStageId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Fase</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió una fase para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!stage || stage.id !== targetStageId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Fase no encontrada</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información de la fase.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate('/panel/fases')}
            sx={{
              mt: 2,
              border: 0,
              background: 'none',
              color: 'primary.main',
              cursor: 'pointer',
              p: 0,
            }}
          >
            Volver al listado
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          alignItems={{ xs: 'flex-start', sm: 'center' }}
          mb={2}
          gap={1}
        >
          <Typography variant="h6">{stage.name}</Typography>
          <Button
            variant="contained"
            color="primary"
            onClick={() => navigate(`/panel/fases/editar/${targetStageId}`)}
          >
            Editar
          </Button>
        </Stack>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Partidos" value="partidos" />
        </Tabs>

        {tab === 'detalle' && (
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Nombre
              </Typography>
              <Typography>{stage.name}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Tipo
              </Typography>
              <Typography>{formatStageType(stage.stageType)}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Activa
              </Typography>
              <Typography>{stage.isActive ? 'Sí' : 'No'}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Eliminación
              </Typography>
              <Typography>{stage.isElimination ? 'Sí' : 'No'}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Inicio
              </Typography>
              <Typography>{formatDate(stage.startDate)}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Fin
              </Typography>
              <Typography>{formatDate(stage.endDate)}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Orden
              </Typography>
              <Typography>{stage.order}</Typography>
            </Grid>
            <Grid item xs={12}>
              <Typography variant="subtitle2" color="text.secondary">
                Descripción
              </Typography>
              <Typography>{stage.description || '—'}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'partidos' && (
          <MatchesPage
            stageId={targetStageId}
            title={undefined}
            wrapInCard={false}
          />
        )}
      </CardContent>
    </Card>
  );
};

export default StagePage;
