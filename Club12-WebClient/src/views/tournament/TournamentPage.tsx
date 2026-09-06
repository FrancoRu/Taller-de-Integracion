import React, { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  Button,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import CategoryChip from '@/views/core/components/CategoryChip';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import {
  TOURNAMENT_CATEGORY_LABELS,
  TournamentCategory,
} from '@/modules/core/enum/tournament/tournamentCategory';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import DivisionsPage from '@/views/division/divisionsPage';
import TeamsPage from '@/views/team/TeamsPage';
import TournamentEnrolledTeams from '@/views/tournament/TournamentEnrolledTeams';
import TournamentDivisionAssignment from '@/views/tournament/TournamentDivisionAssignment';
import { structureToWizardState } from '@/views/tournament/wizard/cloneWizard';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { confirmAction, notifyError } from '@/modules/core/utils/confirmDialog';
import {
  TOURNAMENT_STATUS_LABEL,
  TOURNAMENT_STATUS_COLOR,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { formatCalendarDate } from '@/modules/core/utils/formatDate';
import { runWithBlockingMessage } from '@/modules/core/utils/requestActivity';

const TournamentPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById, putTournamentById, getStructure } =
    useTournament();
  const [loading, setLoading] = useState(false);
  const [reverting, setReverting] = useState(false);
  const [cloneDialogOpen, setCloneDialogOpen] = useState(false);
  const [cloneCategory, setCloneCategory] = useState<TournamentCategory>(
    TournamentCategory.Masculine
  );
  const [cloning, setCloning] = useState(false);
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
          onClick={() => navigate(-1)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver
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
    const ok = await runWithBlockingMessage(
      'Revirtiendo el torneo a borrador. No cierres esta página…',
      () =>
        putTournamentById(tournament.id, {
          name: tournament.name,
          description: tournament.description,
          startDate: new Date(tournament.startDate),
          teamRegistrationDeadline: new Date(
            tournament.teamRegistrationDeadline
          ),
          status: TournamentStatus.RegistrationClosed,
        })
    );

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

  // HU-cloning: opens the category-choice dialog, defaulting to the source's
  // category as a convenience — never silently inherited, always editable
  // before confirming.
  const handleOpenCloneDialog = () => {
    setCloneCategory(tournament.category);
    setCloneDialogOpen(true);
  };

  const handleCloseCloneDialog = () => {
    if (!cloning) {
      setCloneDialogOpen(false);
    }
  };

  const handleConfirmClone = async () => {
    setCloning(true);
    try {
      const structure = await getStructure(tournament.slug || tournament.id);
      if (!structure) {
        await notifyError({
          title: 'No se pudo cargar la estructura del torneo',
          text: 'Volvé a intentar en unos segundos.',
        });
        return;
      }

      const { state: clonePrefill, review: cloneReview } = structureToWizardState(
        structure,
        cloneCategory
      );

      setCloneDialogOpen(false);
      navigate(APP_ROUTES.panelTournamentWizard, {
        state: { clonePrefill, cloneReview },
      });
    } finally {
      setCloning(false);
    }
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
          {canEditTournament && (
            <Button variant="outlined" onClick={handleOpenCloneDialog}>
              Clonar torneo
            </Button>
          )}
          <Button
            variant="outlined"
            // Real browser-history back — the season page (or wherever else
            // this tournament was reached from) with its own tab/sub-nav
            // state intact, instead of always landing on that page's
            // default tab regardless of where "here" actually was.
            onClick={() => navigate(-1)}
          >
            Volver
          </Button>
        </>
      }
    >
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
              <Typography>{formatCalendarDate(tournament.startDate)}</Typography>
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
                {formatCalendarDate(tournament.teamRegistrationDeadline)}
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

      <Dialog open={cloneDialogOpen} onClose={handleCloseCloneDialog} maxWidth="sm" fullWidth>
        <DialogTitle>Clonar torneo</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
            Se creará un torneo nuevo con la misma estructura de zonas, copas y
            puntajes que "{tournament.name}". Nombre, fechas y temporada se
            completan en blanco (o editables) para revisar antes de crear —
            ningún equipo, partido ni resultado se copia.
          </Typography>

          <TextField
            select
            label="Categoría"
            value={cloneCategory}
            onChange={e => setCloneCategory(e.target.value as TournamentCategory)}
            fullWidth
          >
            {Object.values(TournamentCategory).map(category => (
              <MenuItem key={category} value={category}>
                {TOURNAMENT_CATEGORY_LABELS[category]}
              </MenuItem>
            ))}
          </TextField>

          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end', mt: 2 }}>
            <Button color="inherit" onClick={handleCloseCloneDialog} disabled={cloning}>
              Cancelar
            </Button>
            <Button variant="contained" onClick={() => void handleConfirmClone()} disabled={cloning}>
              {cloning ? 'Cargando...' : 'Continuar'}
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </PageShell>
  );
};

export default TournamentPage;
