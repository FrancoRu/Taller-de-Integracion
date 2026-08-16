import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { useTeam } from '@/modules/team/hook/team.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { createInitialWizardState } from './types';
import {
  buildWizardTree,
  validateCrossCupStep,
  validateTeamsStep,
  validateTournamentStep,
  validateZonesStep,
} from './wizardLogic';
import { submitWizard } from './submitWizard';
import TorneoStep from './steps/TorneoStep';
import EquiposStep from './steps/EquiposStep';
import DivisionesStep from './steps/DivisionesStep';
import CopaCruzadaStep from './steps/CopaCruzadaStep';
import RevisionStep from './steps/RevisionStep';

const STEP_LABELS = ['Torneo', 'Equipos', 'Divisiones', 'Copa cruzada', 'Revisión'];

export default function TournamentWizardPage() {
  const navigate = useNavigate();
  const { addTournament, registerTeamsByTournamentId } = useTournament();
  const { teams, getTeamsByFiltered } = useTeam();
  const { addDivision } = useDivision();
  const { addStage, assignTeamsToStage } = useStage();
  const { generateMatchesAutomatically } = useMatch();

  const [activeStep, setActiveStep] = useState(0);
  const [state, setState] = useState(createInitialWizardState);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    void getTeamsByFiltered({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const unregisteredTeams = useMemo(
    () => (teams ?? []).filter(team => !team.tournamentId),
    [teams]
  );

  const selectedTeams = useMemo(
    () => unregisteredTeams.filter(team => state.selectedTeamIds.includes(team.id)),
    [unregisteredTeams, state.selectedTeamIds]
  );

  const treeNodes = useMemo(() => buildWizardTree(state), [state]);

  const stepErrors = useMemo(
    () => [
      validateTournamentStep(state),
      validateTeamsStep(state),
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
        registerTeams: registerTeamsByTournamentId,
        addDivision,
        addStage,
        assignTeamsToStage,
        generateMatches: generateMatchesAutomatically,
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
          text: 'El torneo y su estructura se crearon correctamente.',
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
  }, [
    state,
    addTournament,
    registerTeamsByTournamentId,
    addDivision,
    addStage,
    assignTeamsToStage,
    generateMatchesAutomatically,
    navigate,
  ]);

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" mb={2}>
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
            <EquiposStep
              availableTeams={unregisteredTeams}
              selectedTeamIds={state.selectedTeamIds}
              onChange={selectedTeamIds => setState(prev => ({ ...prev, selectedTeamIds }))}
            />
          )}
          {activeStep === 2 && (
            <DivisionesStep
              teams={selectedTeams}
              zones={state.zones}
              onChange={zones => setState(prev => ({ ...prev, zones }))}
            />
          )}
          {activeStep === 3 && (
            <CopaCruzadaStep
              teams={selectedTeams}
              value={state.crossCup}
              onChange={crossCup => setState(prev => ({ ...prev, crossCup }))}
            />
          )}
          {activeStep === 4 && <RevisionStep nodes={treeNodes} />}
        </Box>

        <Box display="flex" justifyContent="space-between">
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
              {submitting ? 'Creando...' : 'Crear torneo completo'}
            </Button>
          )}
        </Box>
      </CardContent>
    </Card>
  );
}
