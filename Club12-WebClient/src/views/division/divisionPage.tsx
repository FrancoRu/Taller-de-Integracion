import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card, CardContent, Grid, Tab, Tabs, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import LoadingIndicator from '../core/components/LoadingIndicator';
import StagesPage from '../stage/stagesPage';

const DivisionPage: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: GUID }>();
  const navigate = useNavigate();
  const { division, getDivisionsById } = useDivision();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'fases'>('detalle');

  const targetDivisionId = useMemo(
    () => divisionId ?? division?.id,
    [division?.id, divisionId]
  );

  useEffect(() => {
    if (!targetDivisionId) {
      return;
    }

    const fetchDivision = async () => {
      setLoading(true);
      await getDivisionsById(targetDivisionId);
      setLoading(false);
    };

    void fetchDivision();
  }, [getDivisionsById, targetDivisionId]);

  if (!targetDivisionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">División</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió una división para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!division || division.id !== targetDivisionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">División no encontrada</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información de la división.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate('/panel/divisiones')}
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
        <Typography variant="h6" mb={2}>
          {division.name}
        </Typography>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Fases" value="fases" />
        </Tabs>

        {tab === 'detalle' && (
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Nombre
              </Typography>
              <Typography>{division.name}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Estado
              </Typography>
              <Typography>
                {division.isFinished ? 'Finalizada' : 'Activa'}
              </Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Equipos posicionados
              </Typography>
              <Typography>{division.positions?.length ?? 0}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'fases' && (
          <StagesPage
            divisionId={targetDivisionId}
            title={undefined}
            wrapInCard={false}
          />
        )}
      </CardContent>
    </Card>
  );
};

export default DivisionPage;
