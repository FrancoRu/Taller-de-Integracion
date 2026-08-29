import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  Switch,
  TextField,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  IAddStageRequest,
  IStageCreateFormState,
  StageType,
} from '@/modules/stage/type/stage';
import FormButtons from '@/views/core/components/FormButtons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const INITIAL_STAGE_FORM: IStageCreateFormState = {
  name: '',
  description: '',
  stageType: StageType.Group,
  startDate: '',
  endDate: '',
  isActive: true,
  isElimination: false,
  divisionId: '',
  bracketName: '',
  bestOf: 1,
  roundRobinLegs: 1,
};

const BEST_OF_OPTIONS = [1, 3, 5, 7];
const ROUND_ROBIN_LEGS_OPTIONS = [1, 2, 3];

const formatStageType = (value: string) =>
  value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace('Final', 'Final')
    .trim();

const StageCreatePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const queryDivisionId = (searchParams.get('divisionId') ?? '') as GUID | '';

  const { divisions, getDivisionsByFilters } = useDivision();
  const { addStage, getStagesByFilters } = useStage();

  const [submitting, setSubmitting] = useState(false);
  const [stageForm, setStageForm] = useState<IStageCreateFormState>({
    ...INITIAL_STAGE_FORM,
    divisionId: queryDivisionId || '',
  });
  const [divisionHasGroupStage, setDivisionHasGroupStage] = useState(false);

  const isDivisionContext = Boolean(queryDivisionId);

  const loadDivisions = useCallback(async () => {
    if (isDivisionContext) {
      return;
    }

    await getDivisionsByFilters({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [getDivisionsByFilters, isDivisionContext]);

  useEffect(() => {
    void loadDivisions();
  }, [loadDivisions]);

  const divisionOptions = useMemo(() => divisions ?? [], [divisions]);

  const resolvedDivisionId = (queryDivisionId || stageForm.divisionId) as GUID | '';

  /**
   * A division's Group stage represents its whole round-robin phase, so the
   * backend (StageService.CreateStageAsync) rejects a second one regardless
   * of name — mirrored here so the admin sees it before submitting instead
   * of after a 400.
   */
  useEffect(() => {
    if (!resolvedDivisionId) {
      setDivisionHasGroupStage(false);
      return;
    }

    let cancelled = false;

    void (async () => {
      const result = await getStagesByFilters({
        divisionId: resolvedDivisionId,
        stageType: StageType.Group,
        pageSize: FILTER_OPTIONS_PAGE_SIZE,
      });

      if (!cancelled) {
        setDivisionHasGroupStage(Boolean(result?.items.length));
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolvedDivisionId]);

  const handleCancel = () => {
    if (queryDivisionId) {
      navigate(APP_ROUTES.panelDivision.build(queryDivisionId));
      return;
    }

    navigate(APP_ROUTES.panelStages);
  };

  const handleCreate = async () => {
    if (!stageForm.name.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre de la fase es obligatorio.',
      });
      return;
    }

    if (!resolvedDivisionId) {
      await notifyWarning({
        title: 'División requerida',
        text: 'Debes seleccionar una división.',
      });
      return;
    }

    if (stageForm.stageType === StageType.Group && divisionHasGroupStage) {
      await notifyWarning({
        title: 'La división ya tiene fase de grupos',
        text: 'Esta división ya tiene una fase de tipo "Fase de grupos". Elegí otro tipo o editá la existente.',
      });
      return;
    }

    if (!stageForm.startDate || !stageForm.endDate) {
      await notifyWarning({
        title: 'Fechas requeridas',
        text: 'Debes completar fecha de inicio y de fin.',
      });
      return;
    }

    if (new Date(stageForm.endDate) < new Date(stageForm.startDate)) {
      await notifyWarning({
        title: 'Fechas inválidas',
        text: 'La fecha de fin no puede ser anterior a la de inicio.',
      });
      return;
    }

    setSubmitting(true);

    const payload: IAddStageRequest = {
      name: stageForm.name.trim(),
      description: stageForm.description.trim() || undefined,
      stageType: stageForm.stageType,
      isActive: stageForm.isActive,
      isElimination: stageForm.isElimination,
      startDate: new Date(stageForm.startDate),
      endDate: new Date(stageForm.endDate),
      divisionId: resolvedDivisionId as GUID,
      bracketName: stageForm.isElimination
        ? stageForm.bracketName.trim() || undefined
        : undefined,
      bestOf: stageForm.isElimination ? stageForm.bestOf : undefined,
      roundRobinLegs: !stageForm.isElimination ? stageForm.roundRobinLegs : undefined,
    };

    const response = await addStage(payload);
    setSubmitting(false);

    if (!response) {
      return;
    }

    await notifySuccess({
      title: 'Fase creada',
      text: 'La fase se creó correctamente.',
    });

    handleCancel();
  };

  return (
    <PageShell title="Nueva fase" maxWidth="md">
      <Stack spacing={2}>
        <Grid container spacing={2}>
            {!isDivisionContext && (
              <Grid size={12}>
                <TextField
                  select
                  required
                  label="División"
                  value={stageForm.divisionId}
                  onChange={e =>
                    setStageForm(prev => ({
                      ...prev,
                      divisionId: e.target.value as GUID,
                    }))
                  }
                  fullWidth
                >
                  <MenuItem value="" disabled>
                    Seleccionar división
                  </MenuItem>
                  {divisionOptions.map(divisionOption => (
                    <MenuItem key={divisionOption.id} value={divisionOption.id}>
                      {divisionOption.name}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            )}

            <Grid size={12}>
              <TextField
                label="Nombre"
                value={stageForm.name}
                onChange={e =>
                  setStageForm(prev => ({ ...prev, name: e.target.value }))
                }
                required
                fullWidth
              />
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
                minRows={2}
                fullWidth
              />
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                label="Tipo"
                value={stageForm.stageType}
                onChange={e =>
                  setStageForm(prev => ({
                    ...prev,
                    stageType: e.target.value as StageType,
                  }))
                }
                error={stageForm.stageType === StageType.Group && divisionHasGroupStage}
                helperText={
                  divisionHasGroupStage
                    ? 'Esta división ya tiene una fase de grupos: no se puede crear otra.'
                    : undefined
                }
                fullWidth
              >
                {Object.values(StageType).map(stageType => (
                  <MenuItem
                    key={stageType}
                    value={stageType}
                    disabled={stageType === StageType.Group && divisionHasGroupStage}
                  >
                    {formatStageType(stageType)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={stageForm.isActive}
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

            {!stageForm.isElimination && (
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <TextField
                  select
                  label="Veces que se enfrenta cada par (round robin)"
                  value={stageForm.roundRobinLegs}
                  onChange={e =>
                    setStageForm(prev => ({
                      ...prev,
                      roundRobinLegs: Number(e.target.value),
                    }))
                  }
                  fullWidth
                >
                  {ROUND_ROBIN_LEGS_OPTIONS.map(option => (
                    <MenuItem key={option} value={option}>
                      {option === 1 ? 'Una vez (simple)' : `${option} veces`}
                    </MenuItem>
                  ))}
                </TextField>
              </Grid>
            )}

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                label="Inicio"
                type="date"
                value={stageForm.startDate}
                onChange={e =>
                  setStageForm(prev => ({ ...prev, startDate: e.target.value }))
                }
                required
                fullWidth
                slotProps={{
                  inputLabel: { shrink: true }
                }}
              />
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                label="Fin"
                type="date"
                value={stageForm.endDate}
                onChange={e =>
                  setStageForm(prev => ({ ...prev, endDate: e.target.value }))
                }
                required
                fullWidth
                slotProps={{
                  inputLabel: { shrink: true }
                }}
              />
            </Grid>

            <Grid size={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={stageForm.isElimination}
                    onChange={e =>
                      setStageForm(prev => ({
                        ...prev,
                        isElimination: e.target.checked,
                      }))
                    }
                  />
                }
                label="Eliminación"
              />
            </Grid>

            {stageForm.isElimination && (
              <>
                <Grid
                  size={{
                    xs: 12,
                    md: 6
                  }}>
                  <TextField
                    label="Nombre del bracket (opcional)"
                    helperText="Para agrupar rondas paralelas dentro de la misma división (nombre libre, definido por el admin)."
                    value={stageForm.bracketName}
                    onChange={e =>
                      setStageForm(prev => ({
                        ...prev,
                        bracketName: e.target.value,
                      }))
                    }
                    fullWidth
                  />
                </Grid>

                <Grid
                  size={{
                    xs: 12,
                    md: 6
                  }}>
                  <TextField
                    select
                    label="Formato de la serie (Best of)"
                    value={stageForm.bestOf}
                    onChange={e =>
                      setStageForm(prev => ({
                        ...prev,
                        bestOf: Number(e.target.value),
                      }))
                    }
                    fullWidth
                  >
                    {BEST_OF_OPTIONS.map(option => (
                      <MenuItem key={option} value={option}>
                        {option === 1 ? 'Partido único' : `Al mejor de ${option}`}
                      </MenuItem>
                    ))}
                  </TextField>
                </Grid>
              </>
            )}
        </Grid>

        <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
          <FormButtons
            onCancel={handleCancel}
            onConfirm={() => void handleCreate()}
            confirmLabel="Crear"
            disabled={submitting}
          />
        </Stack>
      </Stack>
    </PageShell>
  );
};

export default StageCreatePage;
