import { useCallback, useEffect, useMemo, useState } from 'react';
import { Grid, Stack, TextField, Typography } from '@mui/material';
import { useNavigate, useParams } from 'react-router-dom';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionEditFormState } from '@/modules/playerSanction/type/playerSanction.d';
import FormButtons from '@/views/core/components/FormButtons';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
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

  // The route param is a slug (the detail page's "Editar sanción" button
  // navigates via `playerSanction.slug`), but the fetched record's own `id`
  // is a GUID — comparing only `.id` here meant this never matched and the
  // page always fell through to "Sanción no encontrada", even right after
  // that same slug's own detail fetch had just succeeded.
  const isTargetSanction =
    playerSanction?.id === targetSanctionId || playerSanction?.slug === targetSanctionId;

  useEffect(() => {
    if (!playerSanction || !isTargetSanction) {
      return;
    }

    setForm({
      duration: String(playerSanction.duration ?? ''),
      description: playerSanction.description ?? '',
    });
  }, [playerSanction, isTargetSanction]);

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
      <PageShell title="Editar sanción">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió una sanción para editar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Editar sanción">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!playerSanction || !isTargetSanction) {
    return (
      <PageShell title="Sanción no encontrada">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información de la sanción.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      title="Editar sanción"
      back={{ label: 'Volver', onClick: handleClose }}
    >
        <Stack spacing={2}>
          <Grid container spacing={2}>
            <Grid
              size={{
                xs: 12,
                md: 4
              }}>
              <TextField
                label="Duración (fechas)"
                type="number"
                value={form.duration}
                onChange={e =>
                  setForm(prev => ({ ...prev, duration: e.target.value }))
                }
                required
                fullWidth
                helperText="La duración se expresa en fechas (jornadas)."
                slotProps={{
                  htmlInput: { min: 1 }
                }}
              />
            </Grid>

            <Grid size={12}>
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

          <Stack direction="row" sx={{
            justifyContent: "flex-end"
          }}>
            <FormButtons
              onCancel={handleClose}
              onConfirm={() => void handleSave()}
              confirmLabel="Guardar"
              disabled={submitting}
            />
          </Stack>
        </Stack>
    </PageShell>
  );
};

export default PlayerSanctionEditPage;
