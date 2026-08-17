import React, { useEffect, useState } from 'react';
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
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import DivisionsPage from '@/views/division/divisionsPage';
import TeamsPage from '@/views/team/TeamsPage';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { TOURNAMENT_STATUS_LABEL } from '@/modules/tournament/utils/tournamentDisplay';

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
  }, [tournamentId, tournament?.id, getTournamentById]);

  if (!tournamentId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Torneo</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
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
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información del torneo.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate(APP_ROUTES.panelTournaments)}
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
    if (!tournamentId) {
      return;
    }

    navigate(`${APP_ROUTES.panelDivisionCreate}?tournamentId=${tournamentId}`);
  };

  return (
    <Card>
      <CardContent>
        <Grid
          container
          sx={{
            alignItems: "center",
            justifyContent: "space-between",
            mb: 2
          }}>
          <Grid>
            <Typography variant="h6">{tournament.name}</Typography>
          </Grid>
          <Grid>
            <Stack direction="row" spacing={1}>
              {canEditTournament && (
                <Button
                  variant="contained"
                  onClick={() =>
                    navigate(APP_ROUTES.panelTournamentEdit.build(tournamentId))
                  }
                >
                  Editar torneo
                </Button>
              )}
              <Button
                variant="outlined"
                onClick={() => navigate(APP_ROUTES.panelTournaments)}
              >
                Volver
              </Button>
            </Stack>
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
              <Typography>{tournament.name}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Estado
              </Typography>
              <Typography>
                {
                  TOURNAMENT_STATUS_LABEL[
                    resolveTournamentStatus(tournament.status)
                  ]
                }
              </Typography>
            </Grid>
            <Grid size={12}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Descripción
              </Typography>
              <Typography>{tournament.description || '—'}</Typography>
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
              <Typography>
                {new Date(tournament.startDate).toLocaleDateString('es-AR')}
              </Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Cierre de inscripción
              </Typography>
              <Typography>
                {new Date(
                  tournament.teamRegistrationDeadline
                ).toLocaleDateString('es-AR')}
              </Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Equipos mínimos
              </Typography>
              <Typography>{tournament.minTeams}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
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
