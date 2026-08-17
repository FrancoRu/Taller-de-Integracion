import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import {
  AddPlayerStatisticRequest,
  IPlayerStatisticCreateFormState,
  IPlayerStatisticCreatePageProps,
  StatisticType,
} from '@/modules/playerStatistic/type/playerStatistic.d';
import FormButtons from '@/views/core/components/FormButtons';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

const STATISTIC_TYPE_OPTIONS: StatisticType[] = ['Points', 'Assists'];
const STATISTIC_TYPE_LABELS: Record<StatisticType, string> = {
  Points: 'Puntos',
  Assists: 'Asistencias',
};

const INITIAL_FORM: IPlayerStatisticCreateFormState = {
  value: '',
  type: 'Points',
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

/**
 * Records a player's points or assists for a specific match. Mirrors
 * PlayerSanctionCreatePage's cascading Torneo -> División -> Fase ->
 * Partido -> Equipo -> Jugador picker, since a statistic is always
 * scoped to one match.
 */
const PlayerStatisticCreatePage: React.FC<IPlayerStatisticCreatePageProps> = ({
  open,
  onClose,
  onCreated,
}) => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const { matches, getMatchByFilter } = useMatch();
  const { players, getPlayersByFilter } = usePlayer();
  const { addPlayerStatistic } = usePlayerStatistic();
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<IPlayerStatisticCreateFormState>(INITIAL_FORM);

  useEffect(() => {
    if (!open) {
      return;
    }

    void getAllTournamentsByFilter({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [getAllTournamentsByFilter, open]);

  useEffect(() => {
    if (!open || !form.tournamentId) {
      return;
    }

    void getDivisionsByFilters({
      tournamentId: form.tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.tournamentId, getDivisionsByFilters, open]);

  useEffect(() => {
    if (!open || !form.divisionId) {
      return;
    }

    void getStagesByFilters({
      divisionId: form.divisionId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.divisionId, getStagesByFilters, open]);

  useEffect(() => {
    if (!open || !form.stageId) {
      return;
    }

    void getMatchByFilter({
      stageId: form.stageId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.stageId, getMatchByFilter, open]);

  useEffect(() => {
    if (!open || !form.teamId) {
      return;
    }

    void getPlayersByFilter({
      teamId: form.teamId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
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
      await notifyWarning({ title: 'Campos incompletos', text: 'Debes seleccionar un partido.' });
      return;
    }

    if (!form.teamId) {
      await notifyWarning({ title: 'Campos incompletos', text: 'Debes seleccionar un equipo.' });
      return;
    }

    if (!form.playerId) {
      await notifyWarning({ title: 'Campos incompletos', text: 'Debes seleccionar un jugador.' });
      return;
    }

    if (!form.value || Number(form.value) <= 0) {
      await notifyWarning({ title: 'Valor inválido', text: 'El valor debe ser mayor a 0.' });
      return;
    }

    setSubmitting(true);

    const payload: AddPlayerStatisticRequest = {
      value: Number(form.value),
      matchId: form.matchId,
      playerId: form.playerId,
      type: form.type,
    };

    const createdStatistic = await addPlayerStatistic(payload);
    setSubmitting(false);

    if (!createdStatistic) {
      return;
    }

    await notifySuccess({ title: 'Puntuación creada', text: 'La puntuación se creó correctamente.' });

    setForm(INITIAL_FORM);
    onClose();
    onCreated?.();
  }, [addPlayerStatistic, form, onClose, onCreated]);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Nueva puntuación</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{
          mt: 0.5
        }}>
          <Grid container spacing={2}>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
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
                  <MenuItem key={tournamentOption.id} value={tournamentOption.id}>
                    {tournamentOption.name}
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

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
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

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
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

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                required
                label="Equipo"
                value={form.teamId}
                onChange={e =>
                  setForm(prev => ({ ...prev, teamId: e.target.value as GUID, playerId: '' }))
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

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                required
                label="Jugador"
                value={form.playerId}
                onChange={e => setForm(prev => ({ ...prev, playerId: e.target.value as GUID }))}
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

            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <TextField
                select
                required
                label="Tipo"
                value={form.type}
                onChange={e =>
                  setForm(prev => ({ ...prev, type: e.target.value as StatisticType }))
                }
                fullWidth
              >
                {STATISTIC_TYPE_OPTIONS.map(type => (
                  <MenuItem key={type} value={type}>
                    {STATISTIC_TYPE_LABELS[type]}
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
                label="Valor"
                type="number"
                value={form.value}
                onChange={e => setForm(prev => ({ ...prev, value: e.target.value }))}
                required
                fullWidth
                slotProps={{
                  htmlInput: { min: 1 }
                }}
              />
            </Grid>
          </Grid>

          <Stack direction="row" spacing={1} sx={{
            justifyContent: "flex-end"
          }}>
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

export default PlayerStatisticCreatePage;
