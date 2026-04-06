import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import {
  IAddPlayerSanction,
  IPlayerSanctionCreateFormState,
  IPlayerSanctionCreatePageProps,
} from '@/modules/playerSanction/type/playerSanction.d';
import FormButtons from '@/views/core/components/FormButtons';

const INITIAL_FORM: IPlayerSanctionCreateFormState = {
  duration: '',
  issuedDate: '',
  description: '',
  tournamentId: '',
  divisionId: '',
  stageId: '',
  matchId: '',
  teamId: '',
  playerId: '',
};

const formatDateTime = (value?: string | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleString('es-AR', {
    dateStyle: 'short',
    timeStyle: 'short',
  });
};

const PlayerSanctionCreatePage: React.FC<IPlayerSanctionCreatePageProps> = ({
  open,
  onClose,
  onCreated,
}) => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const { matches, getMatchByFilter } = useMatch();
  const { players, getPlayersByFilter } = usePlayer();
  const { addPlayerSanction } = usePlayerSanction();
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] =
    useState<IPlayerSanctionCreateFormState>(INITIAL_FORM);

  useEffect(() => {
    if (!open) {
      return;
    }

    void getAllTournamentsByFilter({ pageSize: 300 });
  }, [getAllTournamentsByFilter, open]);

  useEffect(() => {
    if (!open || !form.tournamentId) {
      return;
    }

    void getDivisionsByFilters({
      tournamentId: form.tournamentId,
      pageSize: 300,
    });
  }, [form.tournamentId, getDivisionsByFilters, open]);

  useEffect(() => {
    if (!open || !form.divisionId) {
      return;
    }

    void getStagesByFilters({
      divisionId: form.divisionId,
      pageSize: 300,
    });
  }, [form.divisionId, getStagesByFilters, open]);

  useEffect(() => {
    if (!open || !form.stageId) {
      return;
    }

    void getMatchByFilter({
      stageId: form.stageId,
      pageSize: 300,
    });
  }, [form.stageId, getMatchByFilter, open]);

  useEffect(() => {
    if (!open || !form.teamId) {
      return;
    }

    void getPlayersByFilter({
      teamId: form.teamId,
      pageSize: 300,
    });
  }, [form.teamId, getPlayersByFilter, open]);

  const tournamentOptions = useMemo(() => tournaments ?? [], [tournaments]);
  const divisionOptions = useMemo(() => divisions ?? [], [divisions]);
  const stageOptions = useMemo(() => stages ?? [], [stages]);
  const matchOptions = useMemo(() => matches ?? [], [matches]);

  const selectedMatch = useMemo(
    () => matchOptions.find(matchOption => matchOption.id === form.matchId),
    [form.matchId, matchOptions]
  );

  const teamOptions = useMemo(() => {
    const homeTeam = selectedMatch?.homeTeam;
    const visitorTeam = selectedMatch?.visitorTeam;

    return [homeTeam, visitorTeam].filter(
      (team): team is NonNullable<typeof homeTeam> => Boolean(team)
    );
  }, [selectedMatch?.homeTeam, selectedMatch?.visitorTeam]);

  const playerOptions = useMemo(
    () => (form.teamId ? (players ?? []) : []),
    [form.teamId, players]
  );

  const handleClose = useCallback(() => {
    if (submitting) {
      return;
    }

    setForm(INITIAL_FORM);
    onClose();
  }, [onClose, submitting]);

  const handleCreate = useCallback(async () => {
    if (!form.matchId) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un partido.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!form.teamId) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un equipo.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!form.playerId) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un jugador.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!form.duration || Number(form.duration) <= 0) {
      await Swal.fire({
        title: 'Duración inválida',
        text: 'La duración debe ser mayor a 0.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!form.issuedDate) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'Debes completar la fecha de emisión.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    if (!form.description.trim()) {
      await Swal.fire({
        title: 'Campos incompletos',
        text: 'Debes completar la descripción.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
      });
      return;
    }

    setSubmitting(true);

    const payload: IAddPlayerSanction = {
      duration: Number(form.duration),
      issuedDate: new Date(form.issuedDate),
      description: form.description.trim(),
      matchId: form.matchId,
      playerId: form.playerId,
    };

    const createdSanction = await addPlayerSanction(payload);
    setSubmitting(false);

    if (!createdSanction) {
      return;
    }

    await Swal.fire({
      title: 'Sanción creada',
      text: 'La sanción se creó correctamente.',
      icon: 'success',
      confirmButtonColor: '#FD6B00',
    });

    setForm(INITIAL_FORM);
    onClose();
    onCreated?.();
  }, [addPlayerSanction, form, onClose, onCreated]);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Nueva sanción</DialogTitle>
      <DialogContent>
        <Stack spacing={2} mt={0.5}>
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="Torneo"
                value={form.tournamentId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    tournamentId: e.target.value as GUID,
                    divisionId: '',
                    stageId: '',
                    matchId: '',
                    teamId: '',
                    playerId: '',
                  }))
                }
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar torneo
                </MenuItem>
                {tournamentOptions.map(tournamentOption => (
                  <MenuItem
                    key={tournamentOption.id}
                    value={tournamentOption.id}
                  >
                    {tournamentOption.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="División"
                value={form.divisionId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    divisionId: e.target.value as GUID,
                    stageId: '',
                    matchId: '',
                    teamId: '',
                    playerId: '',
                  }))
                }
                disabled={!form.tournamentId}
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

            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="Fase"
                value={form.stageId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    stageId: e.target.value as GUID,
                    matchId: '',
                    teamId: '',
                    playerId: '',
                  }))
                }
                disabled={!form.divisionId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar fase
                </MenuItem>
                {stageOptions.map(stageOption => (
                  <MenuItem key={stageOption.id} value={stageOption.id}>
                    {stageOption.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="Partido"
                value={form.matchId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    matchId: e.target.value as GUID,
                    teamId: '',
                    playerId: '',
                  }))
                }
                disabled={!form.stageId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar partido
                </MenuItem>
                {matchOptions.map(matchOption => (
                  <MenuItem key={matchOption.id} value={matchOption.id}>
                    {`${formatDateTime(matchOption.matchDate)} - ${matchOption.homeTeam?.name ?? '—'} vs ${matchOption.visitorTeam?.name ?? '—'}`}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="Equipo"
                value={form.teamId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    teamId: e.target.value as GUID,
                    playerId: '',
                  }))
                }
                disabled={!form.matchId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar equipo
                </MenuItem>
                {teamOptions.map(teamOption => (
                  <MenuItem key={teamOption.id} value={teamOption.id}>
                    {teamOption.name}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

            <Grid item xs={12} md={6}>
              <TextField
                select
                required
                label="Jugador"
                value={form.playerId}
                onChange={e =>
                  setForm(prev => ({
                    ...prev,
                    playerId: e.target.value as GUID,
                  }))
                }
                disabled={!form.teamId}
                fullWidth
              >
                <MenuItem value="" disabled>
                  Seleccionar jugador
                </MenuItem>
                {playerOptions.map(playerOption => (
                  <MenuItem key={playerOption.id} value={playerOption.id}>
                    {playerOption.fullName}
                  </MenuItem>
                ))}
              </TextField>
            </Grid>

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

            <Grid item xs={12} md={8}>
              <TextField
                label="Fecha de emisión"
                type="datetime-local"
                value={form.issuedDate}
                onChange={e =>
                  setForm(prev => ({ ...prev, issuedDate: e.target.value }))
                }
                InputLabelProps={{ shrink: true }}
                required
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

          <Typography variant="body2" color="text.secondary">
            La duración se expresa en cantidad de partidos.
          </Typography>

          <Stack direction="row" justifyContent="flex-end" spacing={1}>
            <FormButtons
              onCancel={handleClose}
              onConfirm={() => void handleCreate()}
              confirmLabel="Crear"
              disabled={submitting}
            />
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  );
};

export default PlayerSanctionCreatePage;
