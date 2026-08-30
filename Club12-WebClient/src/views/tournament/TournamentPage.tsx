import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, Grid, Stack, Tab, Tabs, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import CategoryChip from '@/views/core/components/CategoryChip';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { TOURNAMENT_CATEGORY_LABELS } from '@/modules/core/enum/tournament/tournamentCategory';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import DivisionsPage from '@/views/division/divisionsPage';
import TeamsPage from '@/views/team/TeamsPage';
import TournamentEnrolledTeams from '@/views/tournament/TournamentEnrolledTeams';
import TournamentDivisionAssignment from '@/views/tournament/TournamentDivisionAssignment';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { confirmAction, notifyError } from '@/modules/core/utils/confirmDialog';
import {
  TOURNAMENT_STATUS_LABEL,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { formatDateAr } from '@/modules/core/utils/formatDate';

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'divisiones' | 'equipos' | 'inscriptos' | 'asignacion'
  >('detalle');

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
      <PageShell title="Torneo">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió un torneo para visualizar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Torneo">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!tournament || (tournament.id !== tournamentId && tournament.slug !== tournamentId)) {
    return (
      <PageShell title="Torneo no encontrado">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información del torneo.
        </Typography>
        <Button
          variant="text"
          onClick={() => navigate(APP_ROUTES.panelTournaments)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver al listado
        </Button>
      </PageShell>
    );
  }

  const canEditTournament =
    role === UserRolesType.Owner || role === UserRolesType.Admin;

  const currentStatus = resolveTournamentStatus(tournament.status);
  const isOngoing = currentStatus === TournamentStatus.Ongoing;

  const handleRevertToDraft = async () => {
    const confirmed = await confirmAction({
      title: 'Revertir a borrador',
      text: 'El torneo vuelve a "Inscripción cerrada" y se ELIMINA el fixture generado (los partidos). Las asignaciones de equipos a zonas se conservan, para que corrijas lo que haga falta y vuelvas a iniciar. Esta acción no se puede deshacer. ¿Continuar?',
      confirmButtonText: 'Revertir a borrador',
    });

    if (!confirmed) {
      return;
    }

    const ok = await putTournamentById(tournament.id, {
      name: tournament.name,
      description: tournament.description,
      startDate: new Date(tournament.startDate),
      teamRegistrationDeadline: new Date(tournament.teamRegistrationDeadline),
      status: TournamentStatus.RegistrationClosed,
    });

    if (ok) {
      await getTournamentById(tournament.id);
    } else {
      await notifyError({
        title: 'No se pudo revertir el torneo',
        text: 'Volvé a intentar en unos segundos.',
      });
    }
  };
  // HU-107: enrolled-team management is the registration phase, only available
  // while the tournament is accepting registrations.
  const isOpenForRegistration =
    currentStatus === TournamentStatus.OpenForRegistration;
  // HU-108/HU-109: division assignment and the "start tournament" gate live in
  // the RegistrationClosed phase, once the fixture skeleton exists.
  const isRegistrationClosed =
    currentStatus === TournamentStatus.RegistrationClosed;
  // Assignment is available as a DRAFT while registration is still open, so the
  // organizer can place teams into zones as teams enroll, and stays available
  // once registration closes (when the tournament can finally start).
  const canAssign = isOpenForRegistration || isRegistrationClosed;

  const handleCreateDivision = () => {
    navigate(`${APP_ROUTES.panelDivisionCreate}?tournamentId=${tournament.id}`);
  };

  return (
    <PageShell
      title={tournament.name}
      actions={
        <>
          {canEditTournament && isOngoing && (
            <Button
              variant="outlined"
              color="warning"
              onClick={() => void handleRevertToDraft()}
            >
              Revertir a borrador
            </Button>
          )}
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
        </>
      }
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2 }}>
        <CategoryChip category={tournament.category} />
      </Stack>

      <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Divisiones" value="divisiones" />
          <Tab label="Equipos" value="equipos" />
          {isOpenForRegistration && (
            <Tab label="Equipos inscriptos" value="inscriptos" />
          )}
          {canAssign && (
            <Tab label="Asignación" value="asignacion" />
          )}
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
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Categoría
              </Typography>
              <Typography>
                {TOURNAMENT_CATEGORY_LABELS[tournament.category]}
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

        {tab === 'inscriptos' && isOpenForRegistration && (
          <TournamentEnrolledTeams tournamentId={tournament.id} />
        )}

        {tab === 'asignacion' && canAssign && (
          <TournamentDivisionAssignment tournament={tournament} />
        )}
    </PageShell>
  );
};

export default TournamentPage;
