import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Card,
  CardContent,
  Grid,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionEditFormState } from '@/modules/playerSanction/type/playerSanction.d';
import FormButtons from '@/views/core/components/FormButtons';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const INITIAL_FORM: IPlayerSanctionEditFormState = {
  duration: '',
  description: '',
};

const PlayerSanctionEditPage: React.FC = () => {
  const navigate = useNavigate();
  const { playerSanctionId } = useParams<{ playerSanctionId: GUID }>();
  const { playerSanction, getPlayerSanctionById, putPlayerSanctionById } =
    usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<IPlayerSanctionEditFormState>(INITIAL_FORM);

  const targetSanctionId = useMemo(
    () => playerSanctionId ?? playerSanction?.id,
    [playerSanction?.id, playerSanctionId]
  );

  useEffect(() => {
    if (!targetSanctionId) {
      return;
    }

    const fetchSanction = async () => {
      setLoading(true);
      await getPlayerSanctionById(targetSanctionId);
      setLoading(false);
    };

    void fetchSanction();
  }, [getPlayerSanctionById, targetSanctionId]);

  useEffect(() => {
    if (!playerSanction || playerSanction.id !== targetSanctionId) {
      return;
    }

    setForm({
      duration: String(playerSanction.duration ?? ''),
      description: playerSanction.description ?? '',
    });
  }, [playerSanction, targetSanctionId]);

  const handleClose = useCallback(() => {
    if (submitting || !targetSanctionId) {
      return;
    }

    navigate(APP_ROUTES.panelSanction.build(targetSanctionId));
  }, [navigate, submitting, targetSanctionId]);

  const handleSave = useCallback(async () => {
    if (!targetSanctionId) {
      return;
    }

    const duration = Number(form.duration);
    const description = form.description.trim();

    if (!Number.isFinite(duration) || duration <= 0) {
      await notifyWarning({
        title: 'Duración inválida',
        text: 'La duración debe ser mayor a 0.',
      });
      return;
    }

    if (!description) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes completar la descripción.',
      });
      return;
    }

    setSubmitting(true);
    const updated = await putPlayerSanctionById(targetSanctionId, {
      duration,
      description,
    });
    setSubmitting(false);

    if (!updated) {
      return;
    }

    await notifySuccess({
      title: 'Sanción actualizada',
      text: 'Los cambios se guardaron correctamente.',
    });

    navigate(APP_ROUTES.panelSanction.build(targetSanctionId));
  }, [
    form.description,
    form.duration,
    navigate,
    putPlayerSanctionById,
    targetSanctionId,
  ]);

  if (!targetSanctionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Editar sanción</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió una sanción para editar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!playerSanction || playerSanction.id !== targetSanctionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Sanción no encontrada</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información de la sanción.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack spacing={2}>
          <Typography variant="h6">Editar sanción</Typography>

          <Grid container spacing={2}>
            <Grid item xs={12} md={4}>
              <TextField
                label="Duración"
                type="number"
                value={form.duration}
                onChange={e =>
                  setForm(prev => ({ ...prev, duration: e.target.value }))
                }
                required
                inputProps={{ min: 1 }}
                fullWidth
              />
            </Grid>

            <Grid item xs={12}>
              <TextField
                label="Descripción"
                value={form.description}
                onChange={e =>
                  setForm(prev => ({ ...prev, description: e.target.value }))
                }
                multiline
                minRows={3}
                required
                fullWidth
              />
            </Grid>
          </Grid>

          <Stack direction="row" justifyContent="flex-end">
            <FormButtons
              onCancel={handleClose}
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

export default PlayerSanctionEditPage;
