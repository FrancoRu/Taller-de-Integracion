import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  FormControlLabel,
  Grid,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { IDivisionPropsView } from '@/modules/division/type/division';
import FormButtons from '@/views/core/components/FormButtons';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

interface IDivisionEditFormState extends IDivisionPropsView {
  isFinished: boolean;
}

const INITIAL_FORM: IDivisionEditFormState = {
  name: '',
  isFinished: false,
};

const DivisionEditPage: React.FC = () => {
  const navigate = useNavigate();
  const { divisionId } = useParams<{ divisionId: GUID }>();
  const { division, getDivisionsById, putDivisionById } = useDivision();

  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<IDivisionEditFormState>(INITIAL_FORM);

  const targetDivisionId = useMemo(
    () => divisionId ?? division?.id,
    [division?.id, divisionId]
  );

  useEffect(() => {
    if (!targetDivisionId) {
      return;
    }

    const fetchDivision = async () => {
      setLoading(true);
      await getDivisionsById(targetDivisionId);
      setLoading(false);
    };

    void fetchDivision();
  }, [getDivisionsById, targetDivisionId]);

  useEffect(() => {
    if (
      !division ||
      (division.id !== targetDivisionId && division.slug !== targetDivisionId)
    ) {
      return;
    }

    setForm({
      name: division.name,
      isFinished: division.isFinished,
    });
  }, [division, targetDivisionId]);

  const handleCancel = useCallback(() => {
    if (!division) {
      navigate(APP_ROUTES.panelDivisions);
      return;
    }

    // Prefer the slug so the detail URL never exposes a UUID (the edit route
    // itself stays id-based).
    navigate(APP_ROUTES.panelDivision.build(division.slug));
  }, [navigate, division]);

  const handleSave = useCallback(async () => {
    if (!division) {
      return;
    }

    if (!form.name.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'El nombre de la división es obligatorio.',
      });
      return;
    }

    setSubmitting(true);
    const success = await putDivisionById(division.id, {
      name: form.name.trim(),
      isFinished: form.isFinished,
    });
    setSubmitting(false);

    if (!success) {
      return;
    }

    await notifySuccess({
      title: 'División actualizada',
      text: 'Los cambios se guardaron correctamente.',
    });

    navigate(APP_ROUTES.panelDivision.build(division.slug));
  }, [division, form.name, form.isFinished, putDivisionById, navigate]);

  if (!targetDivisionId) {
    return (
      <PageShell title="Editar división" maxWidth="md">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió una división para editar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Editar división" maxWidth="md">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (
    !division ||
    (division.id !== targetDivisionId && division.slug !== targetDivisionId)
  ) {
    return (
      <PageShell title="División no encontrada" maxWidth="md">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información de la división.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell title="Editar división" maxWidth="md">
      <Stack spacing={2}>
        <Grid container spacing={2}>
            <Grid size={12}>
              <TextField
                label="Nombre"
                value={form.name}
                onChange={e =>
                  setForm(prev => ({ ...prev, name: e.target.value }))
                }
                required
                fullWidth
              />
            </Grid>

            <Grid size={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={form.isFinished}
                    onChange={e =>
                      setForm(prev => ({
                        ...prev,
                        isFinished: e.target.checked,
                      }))
                    }
                  />
                }
                label="Finalizada"
              />
            </Grid>
        </Grid>

        <Stack direction="row" sx={{ justifyContent: 'flex-end' }}>
          <FormButtons
            onCancel={handleCancel}
            onConfirm={() => void handleSave()}
            confirmLabel="Guardar"
            disabled={submitting}
          />
        </Stack>
      </Stack>
    </PageShell>
  );
};

export default DivisionEditPage;
