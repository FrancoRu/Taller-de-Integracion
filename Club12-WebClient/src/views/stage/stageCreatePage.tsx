import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Card,
  CardContent,
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import {
  IAddStageRequest,
  IStageCreateFormState,
  StageType,
} from '@/modules/stage/type/stage.d';
import FormButtons from '@/views/core/components/FormButtons';

const INITIAL_STAGE_FORM: IStageCreateFormState = {
  name: '',
  description: '',
  stageType: StageType.Group,
  startDate: '',
  endDate: '',
  isActive: true,
  isElimination: false,
  divisionId: '',
};

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
  const { addStage } = useStage();

  const [submitting, setSubmitting] = useState(false);
  const [stageForm, setStageForm] = useState<IStageCreateFormState>({
    ...INITIAL_STAGE_FORM,
    divisionId: queryDivisionId || '',
  });

  const isDivisionContext = Boolean(queryDivisionId);

  const loadDivisions = useCallback(async () => {
    if (isDivisionContext) {
      return;
    }

    await getDivisionsByFilters({ pageSize: 300 });
  }, [getDivisionsByFilters, isDivisionContext]);

  useEffect(() => {
    void loadDivisions();
  }, [loadDivisions]);

  const divisionOptions = useMemo(() => divisions ?? [], [divisions]);

  const handleCancel = () => {
    if (queryDivisionId) {
      navigate(`/panel/divisiones/${queryDivisionId}`);
      return;
    }

    navigate('/panel/fases');
  };

  const handleCreate = async () => {
    const resolvedDivisionId = (queryDivisionId || stageForm.divisionId) as
      | GUID
      | '';

    if (!stageForm.name.trim()) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'El nombre de la fase es obligatorio.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!resolvedDivisionId) {
      await Swal.fire({
        title: 'División requerida',
        text: 'Debes seleccionar una división.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!stageForm.startDate || !stageForm.endDate) {
      await Swal.fire({
        title: 'Fechas requeridas',
        text: 'Debes completar fecha de inicio y de fin.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (new Date(stageForm.endDate) < new Date(stageForm.startDate)) {
      await Swal.fire({
        title: 'Fechas inválidas',
        text: 'La fecha de fin no puede ser anterior a la de inicio.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
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
    };

    const response = await addStage(payload);
    setSubmitting(false);

    if (!response) {
      return;
    }

    await Swal.fire({
      title: 'Fase creada',
      text: 'La fase se creó correctamente.',
      icon: 'success',
      confirmButtonColor: '#FD6B00',
    });

    handleCancel();
  };

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6">Nueva fase</Typography>

          <Grid container spacing={2}>
            {!isDivisionContext && (
              <Grid item xs={12}>
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

            <Grid item xs={12}>
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

            <Grid item xs={12}>
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

            <Grid item xs={12} md={6}>
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
                fullWidth
              >
                {Object.values(StageType).map(stageType => (
                  <MenuItem key={stageType} value={stageType}>
                    {formatStageType(stageType)}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
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

            <Grid item xs={12} md={6}>
              <TextField
                label="Inicio"
                type="date"
                value={stageForm.startDate}
                onChange={e =>
                  setStageForm(prev => ({ ...prev, startDate: e.target.value }))
                }
                InputLabelProps={{ shrink: true }}
                required
                fullWidth
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                label="Fin"
                type="date"
                value={stageForm.endDate}
                onChange={e =>
                  setStageForm(prev => ({ ...prev, endDate: e.target.value }))
                }
                InputLabelProps={{ shrink: true }}
                required
                fullWidth
              />
            </Grid>

            <Grid item xs={12}>
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
          </Grid>

          <Stack direction="row" justifyContent="flex-end">
            <FormButtons
              onCancel={handleCancel}
              onConfirm={() => void handleCreate()}
              confirmLabel="Crear"
              disabled={submitting}
            />
          </Stack>
        </Stack>
      </CardContent>
    </Card>
  );
};

export default StageCreatePage;
