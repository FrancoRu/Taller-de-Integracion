import { useCallback, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Step,
  StepLabel,
  Stepper,
  Typography,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { tournamentService } from '@/modules/tournament/service/tournament.service';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { GUID } from '@/modules/core/types/types';
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
  const { addTournament } = useTournament();
  const { addDivision } = useDivision();
  const { addStage } = useStage();

  const [activeStep, setActiveStep] = useState(0);
  const [state, setState] = useState(createInitialWizardState);
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

  const handleCancel = useCallback(() => {
    navigate(APP_ROUTES.panelTournaments);
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
      const result = await submitWizard(state, {
        addTournament,
        // Structure can only be built while OpenForRegistration (HU-31), and a
        // new tournament starts Scheduled — so open registration right after
        // creating it, before any division/stage. Uses the raw service so the
        // orchestration can tell success from failure (the context method
        // returns void). Errors surface through submitWizard's result.
        openRegistration: async (tournamentId: GUID) => {
          try {
            await tournamentService.putTournamentById(tournamentId, {
              name: state.tournament.name.trim(),
              description: state.tournament.description.trim(),
              startDate: new Date(state.tournament.startDate),
              teamRegistrationDeadline: new Date(
                state.tournament.teamRegistrationDeadline
              ),
              status: TournamentStatus.OpenForRegistration,
            });
            return true;
          } catch {
            return false;
          }
        },
        addDivision,
        addStage,
      });

      if (!result.success) {
        await notifyWarning({
          title: 'No se pudo crear el torneo',
          text: result.error ?? 'Ocurrió un error inesperado.',
        });
        return;
      }

      if (result.warnings.length > 0) {
        await notifyWarning({
          title: 'Torneo creado con observaciones',
          text: `${result.warnings.length} paso(s) no se completaron del todo. Podés terminarlos desde el panel: ${result.warnings[0]}`,
        });
      } else {
        await notifySuccess({
          title: 'Torneo creado',
          text: 'El torneo y su estructura se crearon correctamente. La inscripción quedó abierta: ya podés inscribir equipos.',
        });
      }

      navigate(
        result.tournamentId
          ? APP_ROUTES.panelTournamentDetail.build(result.tournamentId)
          : APP_ROUTES.panelTournaments
      );
    } finally {
      setSubmitting(false);
    }
  }, [state, addTournament, addDivision, addStage, navigate]);

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 2
        }}>
          Asistente de creación de torneo
        </Typography>

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
      </CardContent>
    </Card>
  );
}
