import React, { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Button, Chip, Grid, Stack, Tab, Tabs, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import BlockingOverlay from '@/views/core/components/BlockingOverlay';
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
  TOURNAMENT_STATUS_COLOR,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { formatDateAr } from '@/modules/core/utils/formatDate';

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [reverting, setReverting] = useState(false);
  type TournamentTab = 'detalle' | 'divisiones' | 'equipos' | 'asignacion';
  const TAB_QUERY_PARAM = 'tab';
  // Kept in the URL (not local state) so leaving to a division/team's detail
  // page and clicking "Volver" back here lands on the same tab instead of
  // resetting to Detalle — same pattern as DivisionPage/PublicTournamentPage.
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get(TAB_QUERY_PARAM) ?? 'detalle') as TournamentTab;
  const setTab = (value: TournamentTab) => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.set(TAB_QUERY_PARAM, value);
        return next;
      },
      { replace: true }
    );
  };

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
          onClick={() => navigate(APP_ROUTES.panelSeasons)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver a temporadas
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

    // Block the whole screen while the revert (which tears down the fixture)
    // runs, then hard-reload so every view re-fetches the reverted state.
    setReverting(true);
    const ok = await putTournamentById(tournament.id, {
      name: tournament.name,
      description: tournament.description,
      startDate: new Date(tournament.startDate),
      teamRegistrationDeadline: new Date(tournament.teamRegistrationDeadline),
      status: TournamentStatus.RegistrationClosed,
    });

    if (ok) {
      window.location.reload();
      return;
    }

    setReverting(false);
    await notifyError({
      title: 'No se pudo revertir el torneo',
      text: 'Volvé a intentar en unos segundos.',
    });
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
              disabled={reverting}
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
            onClick={() =>
              navigate(
                tournament.seasonId
                  ? APP_ROUTES.panelSeason.build(
                      tournament.seasonSlug ?? tournament.seasonId
                    )
                  : APP_ROUTES.panelSeasons
              )
            }
          >
            Volver
          </Button>
        </>
      }
    >
      <BlockingOverlay
        open={reverting}
        message="Revirtiendo el torneo a borrador. No cierres esta página…"
      />

      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2 }}>
        <CategoryChip category={tournament.category} />
        <Chip
          label={TOURNAMENT_STATUS_LABEL[currentStatus]}
          color={TOURNAMENT_STATUS_COLOR[currentStatus]}
          size="small"
        />
      </Stack>

      <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Divisiones" value="divisiones" />
          <Tab label="Equipos" value="equipos" />
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
            // Divisions can only be created while the tournament is
            // OpenForRegistration (HU-31, DivisionService.CreateDivisionAsync) —
            // hide the "Nueva División" affordance for every other status
            // instead of showing a button that always 409s.
            createType={isOpenForRegistration ? 'División' : undefined}
            onCreate={handleCreateDivision}
          />
        )}

        {/* While registration is open, "Equipos" IS the registration-management
            view (enroll/unenroll) — showing a second, read-only "Equipos
            inscriptos" tab with the same list was pure duplication. Once
            registration closes, enroll/unenroll is no longer possible, so the
            tab falls back to the plain roster list. */}
        {tab === 'equipos' &&
          (isOpenForRegistration ? (
            <TournamentEnrolledTeams tournamentId={tournament.id} />
          ) : (
            <TeamsPage tournamentId={tournament.id} />
          ))}

        {tab === 'asignacion' && canAssign && (
          <TournamentDivisionAssignment tournament={tournament} />
        )}
    </PageShell>
  );
};

export default TournamentPage;
