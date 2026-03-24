import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card, CardContent, Grid, Tab, Tabs, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import LoadingIndicator from '../core/components/LoadingIndicator';
import DivisionsPage from '../division/divisionsPage';
import TeamsPage from '../team/TeamsPage';
import Swal from 'sweetalert2';

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { tournament, getTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'divisiones' | 'equipos'>(
    'detalle'
  );

  useEffect(() => {
    if (!tournamentId) {
      return;
    }

    const fetchTournament = async () => {
      setLoading(true);
      await getTournamentById(tournamentId);
      setLoading(false);
    };

    void fetchTournament();
  }, [getTournamentById, tournamentId]);

  if (!tournamentId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Torneo</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió un torneo para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!tournament || tournament.id !== tournamentId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Torneo no encontrado</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información del torneo.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate('/panel/torneos')}
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

  const handleCreateDivision = () => {
    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de divisiones desde esta vista aún no está implementada.',
      icon: 'info',
      confirmButtonColor: '#FD6B00',
    });
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" mb={2}>
          {tournament.name}
        </Typography>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Divisiones" value="divisiones" />
          <Tab label="Equipos" value="equipos" />
        </Tabs>

        {tab === 'detalle' && (
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Nombre
              </Typography>
              <Typography>{tournament.name}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Estado
              </Typography>
              <Typography>
                {tournament.isFinished ? 'Finalizado' : 'Activo'}
              </Typography>
            </Grid>
            <Grid item xs={12}>
              <Typography variant="subtitle2" color="text.secondary">
                Descripción
              </Typography>
              <Typography>{tournament.description || '—'}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Inicio
              </Typography>
              <Typography>
                {new Date(tournament.startDate).toLocaleDateString('es-AR')}
              </Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Cierre de inscripción
              </Typography>
              <Typography>
                {new Date(
                  tournament.teamRegistrationDeadline
                ).toLocaleDateString('es-AR')}
              </Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Equipos mínimos
              </Typography>
              <Typography>{tournament.minTeams}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Equipos máximos
              </Typography>
              <Typography>{tournament.maxTeams}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'divisiones' && (
          <DivisionsPage
            tournamentId={tournamentId}
            title={undefined}
            wrapInCard={false}
            createType="División"
            onCreate={handleCreateDivision}
          />
        )}

        {tab === 'equipos' && <TeamsPage tournamentId={tournamentId} />}
      </CardContent>
    </Card>
  );
};

export default TournamentPage;
