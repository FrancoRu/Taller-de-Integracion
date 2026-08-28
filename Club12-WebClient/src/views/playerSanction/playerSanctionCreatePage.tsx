import { useCallback, useEffect, useMemo, useState } from 'react';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
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
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
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
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';

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

const formatDateTime = (value?: string | null) => formatDateTimeAr(value);

const buildInitialForm = (
  presetMatch?: IPlayerSanctionCreatePageProps['presetMatch']
): IPlayerSanctionCreateFormState =>
  presetMatch
    ? {
        ...INITIAL_FORM,
        stageId: presetMatch.stageId ?? '',
        matchId: presetMatch.id,
      }
    : INITIAL_FORM;

const PlayerSanctionCreatePage: React.FC<IPlayerSanctionCreatePageProps> = ({
  open,
  onClose,
  onCreated,
  presetMatch,
}) => {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { divisions, getDivisionsByFilters } = useDivision();
  const { stages, getStagesByFilters } = useStage();
  const { matches, getMatchByFilter } = useMatch();
  const { players, getPlayersByFilter } = usePlayer();
  const { addPlayerSanction } = usePlayerSanction();
  const [submitting, setSubmitting] = useState(false);
  const [form, setForm] = useState<IPlayerSanctionCreateFormState>(() =>
    buildInitialForm(presetMatch)
  );

  // Re-seed the form whenever the dialog (re)opens, so a preset match is
  // always applied and a plain create keeps starting from a blank slate.
  useEffect(() => {
    if (!open) {
      return;
    }

    setForm(buildInitialForm(presetMatch));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, presetMatch?.id]);

  useEffect(() => {
    if (!open || presetMatch) {
      return;
    }

    void getAllTournamentsByFilter({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
  }, [getAllTournamentsByFilter, open, presetMatch]);

  useEffect(() => {
    if (!open || presetMatch || !form.tournamentId) {
      return;
    }

    void getDivisionsByFilters({
      tournamentId: form.tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.tournamentId, getDivisionsByFilters, open, presetMatch]);

  useEffect(() => {
    if (!open || presetMatch || !form.divisionId) {
      return;
    }

    void getStagesByFilters({
      divisionId: form.divisionId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.divisionId, getStagesByFilters, open, presetMatch]);

  useEffect(() => {
    if (!open || presetMatch || !form.stageId) {
      return;
    }

    void getMatchByFilter({
      stageId: form.stageId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
  }, [form.stageId, getMatchByFilter, open, presetMatch]);

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
    () =>
      presetMatch ??
      matchOptions.find(matchOption => matchOption.id === form.matchId),
    [form.matchId, matchOptions, presetMatch]
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

    setForm(buildInitialForm(presetMatch));
    onClose();
  }, [onClose, presetMatch, submitting]);

  const handleCreate = useCallback(async () => {
    if (!form.matchId) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un partido.',
      });
      return;
    }

    if (!form.teamId) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un equipo.',
      });
      return;
    }

    if (!form.playerId) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes seleccionar un jugador.',
      });
      return;
    }

    if (!form.duration || Number(form.duration) <= 0) {
      await notifyWarning({
        title: 'Duración inválida',
        text: 'La duración debe ser mayor a 0.',
      });
      return;
    }

    if (!form.issuedDate) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes completar la fecha de emisión.',
      });
      return;
    }

    if (!form.description.trim()) {
      await notifyWarning({
        title: 'Campos incompletos',
        text: 'Debes completar la descripción.',
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

    await notifySuccess({
      title: 'Sanción creada',
      text: 'La sanción se creó correctamente.',
    });

    setForm(buildInitialForm(presetMatch));
    onClose();
    onCreated?.();
  }, [addPlayerSanction, form, onClose, onCreated, presetMatch]);

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>Nueva sanción</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{
          mt: 0.5
        }}>
          <Grid container spacing={2}>
            {presetMatch && (
              <Grid size={12}>
                <Typography variant="body2" sx={{
                  color: "text.secondary"
                }}>
                  Partido: {presetMatch.homeTeam?.name ?? '—'} vs{' '}
                  {presetMatch.visitorTeam?.name ?? '—'} ·{' '}
                  {formatDateTime(presetMatch.matchDate)}
                </Typography>
              </Grid>
            )}

            {!presetMatch && (
            <>
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
                  <MenuItem
                    key={tournamentOption.id}
                    value={tournamentOption.id}
                  >
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
            </>
            )}

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

            <Grid
              size={{
                xs: 12,
                md: 4
              }}>
              <TextField
                label="Duración"
                type="number"
                value={form.duration}
                onChange={e =>
                  setForm(prev => ({ ...prev, duration: e.target.value }))
                }
                required
                fullWidth
                slotProps={{
                  htmlInput: { min: 1 }
                }}
              />
            </Grid>

            <Grid
              size={{
                xs: 12,
                md: 8
              }}>
              <TextField
                label="Fecha de emisión"
                type="datetime-local"
                value={form.issuedDate}
                onChange={e =>
                  setForm(prev => ({ ...prev, issuedDate: e.target.value }))
                }
                required
                fullWidth
                slotProps={{
                  inputLabel: { shrink: true }
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

          <Typography variant="body2" sx={{
            color: "text.secondary"
          }}>
            La duración se expresa en cantidad de partidos.
          </Typography>

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

export default PlayerSanctionCreatePage;
