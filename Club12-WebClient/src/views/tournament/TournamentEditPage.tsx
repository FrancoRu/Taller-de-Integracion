import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Button, Grid, MenuItem, TextField, Typography } from '@mui/material';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useSeason } from '@/modules/season/hook/season.hook';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { IPutTournamentRequest } from '@/modules/tournament/type/tournament';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  TOURNAMENT_STATUS_LABEL,
  resolveTournamentStatus,
} from '@/modules/tournament/utils/tournamentDisplay';
import { getNextStatusOptions } from '@/modules/tournament/utils/tournamentStatusTransitions';

type TournamentFormState = {
  name: string;
  description: string;
  teamRegistrationDeadline: string;
  startDate: string;
  status: TournamentStatus;
  seasonId: string;
};

const toDateInputValue = (value: Date | string): string => {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '';
  }

  return parsed.toISOString().slice(0, 10);
};

const fromTournamentToForm = (tournament: {
  name: string;
  description: string;
  teamRegistrationDeadline: Date | string;
  startDate: Date | string;
  status: TournamentStatus;
  seasonId?: GUID | null;
}): TournamentFormState => ({
  name: tournament.name,
  description: tournament.description ?? '',
  teamRegistrationDeadline: toDateInputValue(
    tournament.teamRegistrationDeadline
  ),
  startDate: toDateInputValue(tournament.startDate),
  status: resolveTournamentStatus(tournament.status),
  seasonId: tournament.seasonId ?? '',
});

const TournamentEditPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const { seasons, getSeasonsByFiltered } = useSeason();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<TournamentFormState | null>(null);
  const getSeasonsRef = useRef(getSeasonsByFiltered);

  useEffect(() => {
    getSeasonsRef.current = getSeasonsByFiltered;
  }, [getSeasonsByFiltered]);

  useEffect(() => {
    void getSeasonsRef.current({
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
      pageNumber: 1,
    });
  }, []);

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

  useEffect(() => {
    if (!tournament) {
      return;
    }

    setForm(
      fromTournamentToForm({
        ...tournament,
        status: resolveTournamentStatus(tournament.status),
      })
    );
  }, [tournament]);

  const currentStatus = resolveTournamentStatus(tournament?.status);
  const now = new Date();
  const registrationDeadlineDate = new Date(
    tournament?.teamRegistrationDeadline ?? ''
  );
  const registrationClosed =
    !Number.isNaN(registrationDeadlineDate.getTime()) &&
    registrationDeadlineDate.getTime() <= now.getTime();

  const canEditMainFields =
    !registrationClosed && currentStatus === TournamentStatus.Scheduled;
  const canEditRegistrationDeadline =
    !registrationClosed &&
    currentStatus === TournamentStatus.OpenForRegistration;

  // The current status plus the forward-only transitions the backend state
  // machine allows from it. An invalid pick is still rejected server-side (409).
  const statusOptions = useMemo(
    () => [currentStatus, ...getNextStatusOptions(currentStatus)],
    [currentStatus]
  );

  if (!tournamentId) {
    return (
      <PageShell title="Editar torneo">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió un torneo para editar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Editar torneo">
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
          onClick={() => navigate(APP_ROUTES.panelSeasons)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver a temporadas
        </Button>
      </PageShell>
    );
  }

  if (!form) {
    return (
      <PageShell title="Editar torneo">
        <DetailSkeleton />
      </PageShell>
    );
  }

  const handleFormChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = event.target;

    setForm(prev => {
      if (!prev) {
        return prev;
      }

      if (name === 'status') {
        return {
          ...prev,
          status: resolveTournamentStatus(value),
        };
      }

      return {
        ...prev,
        [name]: value,
      };
    });
  };

  const handleCancel = () => {
    navigate(-1);
  };

  const handleSave = async () => {
    if (!tournamentId || !form) {
      return;
    }

    const messages: string[] = [];

    if (canEditMainFields && !form.name.trim()) {
      messages.push('El nombre es obligatorio.');
    }

    if (canEditRegistrationDeadline && !form.teamRegistrationDeadline) {
      messages.push('La fecha de cierre de inscripción es obligatoria.');
    }

    if (!statusOptions.includes(form.status)) {
      messages.push(
        'El estado seleccionado no es válido para tu rol o contexto.'
      );
    }

    if (messages.length > 0) {
      await notifyWarning({
        title: 'No se pudo guardar',
        text: messages[0],
      });
      return;
    }

    const payload: IPutTournamentRequest = {
      name: canEditMainFields ? form.name.trim() : tournament.name,
      description: canEditMainFields
        ? form.description.trim()
        : tournament.description,
      startDate: new Date(form.startDate || tournament.startDate),
      teamRegistrationDeadline: new Date(
        canEditRegistrationDeadline
          ? form.teamRegistrationDeadline
          : tournament.teamRegistrationDeadline
      ),
      status: form.status,
      // Optional grouping into a season ("Temporada"). Empty select detaches
      // the tournament from any season (null).
      seasonId: form.seasonId ? (form.seasonId as GUID) : null,
    };

    setSaving(true);
    try {
      await putTournamentById(tournament.id, payload);
      navigate(APP_ROUTES.panelTournamentDetail.build(tournament.slug ?? tournament.id));
    } finally {
      setSaving(false);
    }
  };

  return (
    <PageShell
      title="Editar torneo"
      actions={
        <>
          <Button variant="outlined" onClick={handleCancel} disabled={saving}>
            Cancelar
          </Button>
          <Button variant="contained" onClick={handleSave} disabled={saving}>
            {saving ? 'Guardando...' : 'Guardar cambios'}
          </Button>
        </>
      }
    >
      <Grid container spacing={2}>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <TextField
              fullWidth
              name="name"
              size="small"
              label="Nombre"
              value={form.name}
              onChange={handleFormChange}
              disabled={!canEditMainFields}
            />
          </Grid>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <TextField
              select
              fullWidth
              name="status"
              size="small"
              label="Estado"
              value={form.status}
              onChange={handleFormChange}
            >
              {statusOptions.map(status => (
                <MenuItem key={status} value={status}>
                  {TOURNAMENT_STATUS_LABEL[status]}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid size={12}>
            <TextField
              fullWidth
              multiline
              minRows={2}
              name="description"
              size="small"
              label="Descripción"
              value={form.description}
              onChange={handleFormChange}
              disabled={!canEditMainFields}
            />
          </Grid>

          <Grid size={12}>
            <TextField
              select
              fullWidth
              name="seasonId"
              size="small"
              label="Temporada"
              value={form.seasonId}
              onChange={handleFormChange}
              helperText="Agrupá el torneo dentro de una temporada. Podés dejarlo vacío."
            >
              <MenuItem value="">
                <em>Sin temporada</em>
              </MenuItem>
              {(seasons ?? []).map(season => (
                <MenuItem key={season.id} value={season.id}>
                  {season.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>

          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <TextField
              fullWidth
              name="startDate"
              size="small"
              type="date"
              label="Inicio"
              value={form.startDate}
              disabled
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
              fullWidth
              name="teamRegistrationDeadline"
              size="small"
              type="date"
              label="Cierre de inscripción"
              value={form.teamRegistrationDeadline}
              onChange={handleFormChange}
              disabled={!canEditRegistrationDeadline}
              slotProps={{
                inputLabel: { shrink: true }
              }}
            />
          </Grid>
        </Grid>
    </PageShell>
  );
};

export default TournamentEditPage;
