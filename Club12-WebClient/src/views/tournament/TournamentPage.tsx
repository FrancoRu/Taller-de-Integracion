import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Grid,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import theme from '@/theme';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import DivisionsPage from '@/views/division/divisionsPage';
import TeamsPage from '@/views/team/TeamsPage';

const TOURNAMENT_STATUS_LABELS: Record<TournamentStatus, string> = {
  [TournamentStatus.Scheduled]: 'Programado',
  [TournamentStatus.OpenForRegistration]: 'Inscripción abierta',
  [TournamentStatus.Ongoing]: 'En curso',
  [TournamentStatus.Finished]: 'Finalizado',
  [TournamentStatus.Canceled]: 'Cancelado',
};

const resolveTournamentStatus = (status: unknown): TournamentStatus => {
  if (typeof status === 'string') {
    if (
      status === TournamentStatus.Scheduled ||
      status === TournamentStatus.OpenForRegistration ||
      status === TournamentStatus.Ongoing ||
      status === TournamentStatus.Finished ||
      status === TournamentStatus.Canceled
    ) {
      return status;
    }
  }

  return TournamentStatus.Scheduled;
};

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'divisiones' | 'equipos'>(
    'detalle'
  );

  useEffect(() => {
    if (!tournamentId) {
      return;
    }

    if (tournament?.id === tournamentId) {
      return;
    }

    const fetchTournament = async () => {
      setLoading(true);
      try {
        await getTournamentById(tournamentId);
      } finally {
        setLoading(false);
      }
    };

    void fetchTournament();
  }, [tournamentId, tournament?.id]);

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

  const canEditTournament =
    role === UserRolesType.Owner || role === UserRolesType.TournamentManager;

  const handleCreateDivision = () => {
    void Swal.fire({
      title: 'Pendiente',
      text: 'La creación de divisiones desde esta vista aún no está implementada.',
      icon: 'info',
      confirmButtonColor: theme.palette.primary.main,
    });
  };

  return (
    <Card>
      <CardContent>
        <Grid
          container
          alignItems="center"
          justifyContent="space-between"
          mb={2}
        >
          <Grid item>
            <Typography variant="h6">{tournament.name}</Typography>
          </Grid>
          <Grid item>
            {canEditTournament && (
              <Button
                variant="contained"
                onClick={() =>
                  navigate(`/panel/torneos/${tournamentId}/editar`)
                }
              >
                Editar torneo
              </Button>
            )}
          </Grid>
        </Grid>

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
                {
                  TOURNAMENT_STATUS_LABELS[
                    resolveTournamentStatus(tournament.status)
                  ]
                }
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
