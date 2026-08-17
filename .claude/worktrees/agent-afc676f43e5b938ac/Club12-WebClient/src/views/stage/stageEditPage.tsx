import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Card,
  CardContent,
  FormControlLabel,
  Grid,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { IStageEditFormState } from '@/modules/stage/type/stage';
import FormButtons from '@/views/core/components/FormButtons';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const INITIAL_FORM: IStageEditFormState = {
  description: '',
  isActive: false,
};

/**
 * If the backend sends a date-only format (no time component), the end date
 * is treated as passed only after that whole day ends, not at midnight.
 */
const hasEndDatePassed = (endDateValue?: string | null): boolean => {
  if (!endDateValue) {
    return false;
  }

  const parsed = new Date(endDateValue);
  if (Number.isNaN(parsed.getTime())) {
    return false;
  }

  if (!endDateValue.includes('T')) {
    parsed.setHours(23, 59, 59, 999);
  }

  return parsed.getTime() < Date.now();
};

const StageEditPage: React.FC = () => {
  const navigate = useNavigate();
  const { stageId } = useParams<{ stageId: GUID }>();
  const { stage, getStageById, putStageById } = useStage();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [stageForm, setStageForm] = useState<IStageEditFormState>(INITIAL_FORM);

  const targetStageId = useMemo(
    () => stageId ?? stage?.id,
    [stage?.id, stageId]
  );

  useEffect(() => {
    if (!targetStageId) {
      return;
    }

    const fetchStage = async () => {
      setLoading(true);
      await getStageById(targetStageId);
      setLoading(false);
    };

    void fetchStage();
  }, [getStageById, targetStageId]);

  useEffect(() => {
    if (!stage || stage.id !== targetStageId) {
      return;
    }

    setStageForm({
      description: stage.description ?? '',
      isActive: stage.isActive,
    });
  }, [stage, targetStageId]);

  const canEditIsActive = useMemo(() => {
    return hasEndDatePassed(stage?.endDate);
  }, [stage?.endDate]);

  const handleCancel = useCallback(() => {
    if (!targetStageId) {
      navigate(APP_ROUTES.panelStages);
      return;
    }

    navigate(APP_ROUTES.panelStage.build(targetStageId));
  }, [navigate, targetStageId]);

  const handleSave = useCallback(async () => {
    if (!targetStageId || !stage) {
      return;
    }

    if (!canEditIsActive && stageForm.isActive !== stage.isActive) {
      await notifyWarning({
        title: 'Cambio no permitido',
        text: 'Solo podés modificar el estado Activa cuando la fecha de fin ya pasó.',
      });
      return;
    }

    setSubmitting(true);
    const success = await putStageById(targetStageId, {
      description: stageForm.description.trim() || null,
      isActive: canEditIsActive ? stageForm.isActive : stage.isActive,
    });
    setSubmitting(false);

    if (!success) {
      return;
    }

    await notifySuccess({
      title: 'Fase actualizada',
      text: 'Los cambios se guardaron correctamente.',
    });

    navigate(APP_ROUTES.panelStage.build(targetStageId));
  }, [
    canEditIsActive,
    navigate,
    putStageById,
    stage,
    stageForm.description,
    stageForm.isActive,
    targetStageId,
  ]);

  if (!targetStageId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Editar fase</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió una fase para editar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!stage || stage.id !== targetStageId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Fase no encontrada</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información de la fase.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6">Editar fase</Typography>

          {!canEditIsActive && (
            <Alert severity="info">
              El estado Activa solo puede modificarse cuando la fecha de fin de
              la fase ya pasó.
            </Alert>
          )}

          <Grid container spacing={2}>
            <Grid size={12}>
              <TextField label="Nombre" value={stage.name} disabled fullWidth />
            </Grid>

            <Grid size={12}>
              <TextField
                label="Descripción"
                value={stageForm.description}
                onChange={e =>
                  setStageForm(prev => ({
                    ...prev,
                    description: e.target.value,
                  }))
                }
                multiline
                minRows={3}
                fullWidth
              />
            </Grid>

            <Grid size={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={stageForm.isActive}
                    disabled={!canEditIsActive}
                    onChange={e =>
                      setStageForm(prev => ({
                        ...prev,
                        isActive: e.target.checked,
                      }))
                    }
                  />
                }
                label="Activa"
              />
            </Grid>
          </Grid>

          <Stack direction="row" sx={{
            justifyContent: "flex-end"
          }}>
            <FormButtons
              onCancel={handleCancel}
              onConfirm={() => void handleSave()}
              confirmLabel="Guardar"
              disabled={submitting}
            />
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default StageEditPage;
