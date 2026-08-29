import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, Grid, Tab, Tabs, Typography } from '@mui/material';
import { useStage } from '@/modules/stage/hook/stage.hook';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import MatchesPage from '@/views/match/matchesPage';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

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
  const { stageId } = useParams<{ stageId: string }>();
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
      <PageShell title="Fase">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió una fase para visualizar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Fase">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (
    !stage ||
    (stage.id !== targetStageId && stage.slug !== targetStageId)
  ) {
    return (
      <PageShell title="Fase no encontrada">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información de la fase.
        </Typography>
        <Button
          variant="text"
          onClick={() => navigate(APP_ROUTES.panelStages)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver al listado
        </Button>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={stage.name}
      actions={
        <Button
          variant="contained"
          color="primary"
          onClick={() => navigate(APP_ROUTES.panelStages)}
        >
          Volver
        </Button>
      }
    >
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
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Nombre
              </Typography>
              <Typography>{stage.name}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Tipo
              </Typography>
              <Typography>{formatStageType(stage.stageType)}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Activa
              </Typography>
              <Typography>{stage.isActive ? 'Sí' : 'No'}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Eliminación
              </Typography>
              <Typography>{stage.isElimination ? 'Sí' : 'No'}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Inicio
              </Typography>
              <Typography>{formatDate(stage.startDate)}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Fin
              </Typography>
              <Typography>{formatDate(stage.endDate)}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Orden
              </Typography>
              <Typography>{stage.order}</Typography>
            </Grid>
            <Grid size={12}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Descripción
              </Typography>
              <Typography>{stage.description || '—'}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'partidos' && (
          <MatchesPage
            stageId={stage.id}
            title={undefined}
            wrapInCard={false}
          />
        )}
    </PageShell>
  );
};

export default StagePage;
