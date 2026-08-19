import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Grid,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import { IPutTournamentRequest } from '@/modules/tournament/type/tournament';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { TOURNAMENT_STATUS_LABEL } from '@/modules/tournament/utils/tournamentDisplay';

const STATUS_FLOW_ORDER: Record<TournamentStatus, number> = {
  [TournamentStatus.Scheduled]: 0,
  [TournamentStatus.OpenForRegistration]: 1,
  [TournamentStatus.Ongoing]: 2,
  [TournamentStatus.Finished]: 3,
  [TournamentStatus.Canceled]: 4,
};

const resolveTournamentStatus = (status: unknown): TournamentStatus => {
  if (typeof status === 'string') {
    if (
      status === TournamentStatus.Scheduled ||
      status === TournamentStatus.OpenForRegistration ||
      status === TournamentStatus.Ongoing ||
      status === TournamentStatus.Finished ||
      status === TournamentStatus.Canceled
    ) {
      return status;
    }
  }

  return TournamentStatus.Scheduled;
};

type TournamentFormState = {
  name: string;
  description: string;
  minTeams: number;
  maxTeams: number;
  teamRegistrationDeadline: string;
  startDate: string;
  status: TournamentStatus;
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
  minTeams: number;
  maxTeams: number;
  teamRegistrationDeadline: Date | string;
  startDate: Date | string;
  status: TournamentStatus;
}): TournamentFormState => ({
  name: tournament.name,
  description: tournament.description ?? '',
  minTeams: tournament.minTeams,
  maxTeams: tournament.maxTeams,
  teamRegistrationDeadline: toDateInputValue(
    tournament.teamRegistrationDeadline
  ),
  startDate: toDateInputValue(tournament.startDate),
  status: resolveTournamentStatus(tournament.status),
});

const TournamentEditPage: React.FC = () => {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { tournament, getTournamentById, putTournamentById } = useTournament();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<TournamentFormState | null>(null);

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

  const isOwner = role === UserRolesType.Owner;
  const currentStatus = resolveTournamentStatus(tournament?.status);
  const now = new Date();
  const registrationDeadlineDate = new Date(
    tournament?.teamRegistrationDeadline ?? ''
  );
  const startDate = new Date(tournament?.startDate ?? '');
  const registrationClosed =
    !Number.isNaN(registrationDeadlineDate.getTime()) &&
    registrationDeadlineDate.getTime() <= now.getTime();
  const tournamentStarted =
    !Number.isNaN(startDate.getTime()) && startDate.getTime() <= now.getTime();

  const canEditMainFields =
    !registrationClosed && currentStatus === TournamentStatus.Scheduled;
  const canEditRegistrationDeadline =
    !registrationClosed &&
    currentStatus === TournamentStatus.OpenForRegistration;

  const statusOptions = useMemo(() => {
    const allStatuses: TournamentStatus[] = [
      TournamentStatus.Scheduled,
      TournamentStatus.OpenForRegistration,
      TournamentStatus.Ongoing,
      TournamentStatus.Finished,
      TournamentStatus.Canceled,
    ];

    return allStatuses.filter(status => {
      if (status === currentStatus) {
        return true;
      }

      if (status === TournamentStatus.Canceled) {
        return isOwner || !tournamentStarted;
      }

      if (currentStatus === TournamentStatus.Canceled) {
        return false;
      }

      return STATUS_FLOW_ORDER[status] >= STATUS_FLOW_ORDER[currentStatus];
    });
  }, [currentStatus, isOwner, tournamentStarted]);

  if (!tournamentId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Torneo</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió un torneo para editar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!tournament || (tournament.id !== tournamentId && tournament.slug !== tournamentId)) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Torneo no encontrado</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información del torneo.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate(APP_ROUTES.panelTournaments)}
            sx={{
              mt: 2,
              border: 0,
              background: 'none',
              color: 'primary.main',
              cursor: 'pointer',
              p: 0,
            }}
          >
            Volver al listado
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (!form) {
    return <LoadingIndicator />;
  }

  const handleFormChange = (
    event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>
  ) => {
    const { name, value } = event.target;

    setForm(prev => {
      if (!prev) {
        return prev;
      }

      if (name === 'minTeams' || name === 'maxTeams') {
        return {
          ...prev,
          [name]: Number(value),
        };
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
    navigate(APP_ROUTES.panelTournamentDetail.build(tournamentId));
  };

  const handleSave = async () => {
    if (!tournamentId || !form) {
      return;
    }

    const messages: string[] = [];

    if (canEditMainFields) {
      if (!form.name.trim()) {
        messages.push('El nombre es obligatorio.');
      }

      if (form.minTeams <= 0 || form.maxTeams <= 0) {
        messages.push('Los equipos mínimos y máximos deben ser mayores a 0.');
      }

      if (form.minTeams > form.maxTeams) {
        messages.push('Los equipos mínimos no pueden superar a los máximos.');
      }
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
      minTeams: canEditMainFields ? form.minTeams : tournament.minTeams,
      maxTeams: canEditMainFields ? form.maxTeams : tournament.maxTeams,
      startDate: new Date(form.startDate || tournament.startDate),
      teamRegistrationDeadline: new Date(
        canEditRegistrationDeadline
          ? form.teamRegistrationDeadline
          : tournament.teamRegistrationDeadline
      ),
      status: form.status,
    };

    setSaving(true);
    try {
      await putTournamentById(tournament.id, payload);
      navigate(APP_ROUTES.panelTournamentDetail.build(tournament.id));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardContent>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          sx={{
            justifyContent: "space-between",
            alignItems: { xs: 'flex-start', sm: 'center' },
            mb: 2
          }}>
          <Typography variant="h6">Editar torneo</Typography>
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" onClick={handleCancel} disabled={saving}>
              Cancelar
            </Button>
            <Button variant="contained" onClick={handleSave} disabled={saving}>
              {saving ? 'Guardando...' : 'Guardar cambios'}
            </Button>
          </Stack>
        </Stack>

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

          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <TextField
              fullWidth
              name="minTeams"
              size="small"
              type="number"
              label="Equipos mínimos"
              value={form.minTeams}
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
              fullWidth
              name="maxTeams"
              size="small"
              type="number"
              label="Equipos máximos"
              value={form.maxTeams}
              onChange={handleFormChange}
              disabled={!canEditMainFields}
            />
          </Grid>
        </Grid>
      </CardContent>
    </Card>
  );
};

export default TournamentEditPage;
