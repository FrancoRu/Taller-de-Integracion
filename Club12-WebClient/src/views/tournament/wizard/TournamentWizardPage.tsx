import { useCallback, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { Box, Button, Step, StepLabel, Stepper } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { createInitialWizardState } from './types';
import {
  buildWizardTree,
  validateCrossCupStep,
  validateTournamentStep,
  validateZonesStep,
} from './wizardLogic';
import { submitWizard } from './submitWizard';
import TorneoStep from './steps/TorneoStep';
import DivisionesStep from './steps/DivisionesStep';
import CopaCruzadaStep from './steps/CopaCruzadaStep';
import RevisionStep from './steps/RevisionStep';

// HU-106: the wizard creates the tournament + its division/zone/cup/stage
// structure only. Teams are inscribed later (registration phase) and
// assigned to divisions once registration closes — so there is no "Equipos"
// step here anymore.
const STEP_LABELS = ['Torneo', 'Divisiones', 'Copa cruzada', 'Revisión'];

export default function TournamentWizardPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { createFullTournament } = useTournament();

  // The wizard is normally launched pre-scoped to a season (from the admin
  // season hub) via router state, which locks the season in the first step. A
  // standalone launch has no state and must pick a season before continuing —
  // a tournament always belongs to a season.
  const seededSeasonId =
    (location.state as { seasonId?: string } | null)?.seasonId ?? '';

  const [activeStep, setActiveStep] = useState(0);
  const [state, setState] = useState(() => {
    const initial = createInitialWizardState();
    if (!seededSeasonId) {
      return initial;
    }
    return {
      ...initial,
      tournament: { ...initial.tournament, seasonId: seededSeasonId },
    };
  });
  const [submitting, setSubmitting] = useState(false);

  const treeNodes = useMemo(() => buildWizardTree(state), [state]);

  const stepErrors = useMemo(
    () => [
      validateTournamentStep(state),
      validateZonesStep(state),
      validateCrossCupStep(state),
      [],
    ],
    [state]
  );

  // Real browser-history back — takes the admin back to exactly the page
  // (and tab/sub-nav state) they started the wizard from.
  const handleCancel = useCallback(() => {
    navigate(-1);
  }, [navigate]);

  const handleNext = useCallback(async () => {
    const errors = stepErrors[activeStep];
    if (errors.length > 0) {
      await notifyWarning({ title: 'Revisá este paso', text: errors[0] });
      return;
    }

    setActiveStep(prev => Math.min(prev + 1, STEP_LABELS.length - 1));
  }, [activeStep, stepErrors]);

  const handleBack = useCallback(() => {
    setActiveStep(prev => Math.max(prev - 1, 0));
  }, []);

  const handleSubmit = useCallback(async () => {
    setSubmitting(true);
    try {
      // HU-38: the whole tournament (base fields + every division/zone with its
      // points, cups, playoff mappings and stages) is created in ONE atomic
      // backend call. All-or-nothing — a single failure leaves no partial
      // tournament behind — and the backend creates it already
      // OpenForRegistration, so there is no separate open-registration call.
      const result = await submitWizard(state, { createFullTournament });

      if (!result.success) {
        await notifyWarning({
          title: 'No se pudo crear el torneo',
          text: result.error ?? 'Ocurrió un error inesperado.',
        });
        return;
      }

      await notifySuccess({
        title: 'Torneo creado',
        text: 'El torneo y su estructura se crearon correctamente. La inscripción quedó abierta: ya podés inscribir equipos.',
      });

      // Prefer the created tournament's slug for the detail route, falling
      // back to its id — a created tournament always has at least one, so
      // this is only ever missing in an impossible/defensive case.
      const detailKey = result.slug ?? result.tournamentId;
      if (detailKey) {
        navigate(APP_ROUTES.panelTournamentDetail.build(detailKey));
      }
    } finally {
      setSubmitting(false);
    }
  }, [state, createFullTournament, navigate]);

  return (
    <PageShell title="Asistente de creación de torneo">
      <Stepper activeStep={activeStep} sx={{ mb: 3 }}>
          {STEP_LABELS.map(label => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        <Box sx={{ minHeight: 240, mb: 3 }}>
          {activeStep === 0 && (
            <TorneoStep
              value={state.tournament}
              onChange={tournament => setState(prev => ({ ...prev, tournament }))}
              seasonPreset={Boolean(seededSeasonId)}
            />
          )}
          {activeStep === 1 && (
            <DivisionesStep
              zones={state.zones}
              onChange={zones => setState(prev => ({ ...prev, zones }))}
            />
          )}
          {activeStep === 2 && (
            <CopaCruzadaStep
              value={state.crossCup}
              onChange={crossCup => setState(prev => ({ ...prev, crossCup }))}
            />
          )}
          {activeStep === 3 && <RevisionStep nodes={treeNodes} />}
        </Box>

        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between"
          }}>
          <Button onClick={activeStep === 0 ? handleCancel : handleBack} disabled={submitting}>
            {activeStep === 0 ? 'Cancelar' : 'Atrás'}
          </Button>

          {activeStep < STEP_LABELS.length - 1 ? (
            <Button variant="contained" onClick={() => void handleNext()} disabled={submitting}>
              Continuar
            </Button>
          ) : (
            <Button
              variant="contained"
              onClick={() => void handleSubmit()}
              disabled={submitting}
            >
              {submitting ? 'Creando...' : 'Crear torneo'}
            </Button>
          )}
        </Box>
    </PageShell>
  );
}
