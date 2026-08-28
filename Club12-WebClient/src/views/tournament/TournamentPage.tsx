import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Divider,
  Grid,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
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
import {
  TOURNAMENT_STATUS_LABEL,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { getNextStatusOptions } from '@/modules/tournament/utils/tournamentStatusTransitions';
import { IPutTournamentRequest } from '@/modules/tournament/type/tournament';
import { confirmAction } from '@/modules/core/utils/confirmDialog';
import { formatDateAr } from '@/modules/core/utils/formatDate';

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'divisiones' | 'equipos'>(
    'detalle'
  );
  const [targetStatus, setTargetStatus] = useState<TournamentStatus | ''>('');
  const [changingStatus, setChangingStatus] = useState(false);

  useEffect(() => {
    if (!tournamentId) {
      return;
    }

    if (tournament?.id === tournamentId || tournament?.slug === tournamentId) {
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
  }, [tournamentId, tournament?.id, tournament?.slug, getTournamentById]);

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

  if (!tournament || (tournament.id !== tournamentId && tournament.slug !== tournamentId)) {
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

  const currentStatus = resolveTournamentStatus(tournament.status);
  const nextStatusOptions = getNextStatusOptions(currentStatus);
  const canChangeStatus = canEditTournament && nextStatusOptions.length > 0;

  const handleCreateDivision = () => {
    navigate(`${APP_ROUTES.panelDivisionCreate}?tournamentId=${tournament.id}`);
  };

  const handleChangeStatus = async () => {
    if (!targetStatus) {
      return;
    }

    const label = TOURNAMENT_STATUS_LABEL[targetStatus];

    // Closing registration is the fixture-generation trigger on the backend;
    // make that consequence explicit before the admin commits to it.
    const confirmed =
      targetStatus === TournamentStatus.RegistrationClosed
        ? await confirmAction({
            title: 'Cerrar inscripción',
            text: 'Se cerrará la inscripción y se generará automáticamente el fixture del torneo. Esta acción no se puede revertir. ¿Continuar?',
            confirmButtonText: 'Cerrar y generar fixture',
          })
        : await confirmAction({
            title: `Cambiar estado a "${label}"`,
            text: 'El cambio de estado sigue el flujo del torneo y no se puede revertir. ¿Continuar?',
            confirmButtonText: 'Cambiar estado',
          });

    if (!confirmed) {
      return;
    }

    // The status-change endpoint is the tournament PUT: the backend routes the
    // requested status through its forward-only state machine and rejects an
    // invalid transition with 409, which the global error handler surfaces to
    // the user. Only valid next statuses are offered above, so a 409 here only
    // happens on a stale view (someone else advanced the tournament first).
    const payload: IPutTournamentRequest = {
      name: tournament.name,
      description: tournament.description,
      startDate: new Date(tournament.startDate),
      teamRegistrationDeadline: new Date(tournament.teamRegistrationDeadline),
      status: targetStatus,
    };

    setChangingStatus(true);
    try {
      await putTournamentById(tournament.id, payload);
      setTargetStatus('');
    } finally {
      setChangingStatus(false);
    }
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
              <Typography>{TOURNAMENT_STATUS_LABEL[currentStatus]}</Typography>
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
              <Typography>{formatDateAr(tournament.startDate)}</Typography>
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
                {formatDateAr(tournament.teamRegistrationDeadline)}
              </Typography>
            </Grid>

            {canChangeStatus && (
              <Grid size={12}>
                <Divider sx={{ my: 1 }} />
                <Typography variant="subtitle2" sx={{ mb: 1 }}>
                  Cambiar estado
                </Typography>
                <Stack
                  direction={{ xs: 'column', sm: 'row' }}
                  spacing={1}
                  sx={{ alignItems: { sm: 'center' } }}
                >
                  <TextField
                    select
                    size="small"
                    label="Nuevo estado"
                    value={targetStatus}
                    onChange={event =>
                      setTargetStatus(event.target.value as TournamentStatus)
                    }
                    sx={{ minWidth: 220 }}
                  >
                    {nextStatusOptions.map(status => (
                      <MenuItem key={status} value={status}>
                        {TOURNAMENT_STATUS_LABEL[status]}
                      </MenuItem>
                    ))}
                  </TextField>
                  <Button
                    variant="contained"
                    onClick={handleChangeStatus}
                    disabled={!targetStatus || changingStatus}
                  >
                    {changingStatus ? 'Aplicando...' : 'Aplicar'}
                  </Button>
                </Stack>
                {targetStatus === TournamentStatus.RegistrationClosed && (
                  <Typography
                    variant="caption"
                    sx={{ color: 'text.secondary', display: 'block', mt: 1 }}
                  >
                    Al cerrar la inscripción se generará automáticamente el
                    fixture del torneo.
                  </Typography>
                )}
              </Grid>
            )}
          </Grid>
        )}

        {tab === 'divisiones' && (
          <DivisionsPage
            tournamentId={tournament.id}
            title={undefined}
            wrapInCard={false}
            createType="División"
            onCreate={handleCreateDivision}
          />
        )}

        {tab === 'equipos' && <TeamsPage tournamentId={tournament.id} />}
      </CardContent>
    </Card>
  );
};

export default TournamentPage;
