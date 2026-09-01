import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { ListSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { useMatch } from '@/modules/match/hook/match.hook';
import { IMatchResponse, IRoundMatchesResponse } from '@/modules/match/type/match';
import {
  BYE_TEAM_LABEL,
  byeTeamNamesForRound,
  collectStageTeamNames,
  formatRoundLabel,
} from '@/modules/match/utils/matchGrouping';
import { formatMatchScore } from '@/modules/match/utils/matchDisplay';
import { formatDateTimeAr } from '@/modules/core/utils/formatDate';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchStatusChip from '@/views/match/MatchStatusChip';
import { EventBusyIcon } from '@/views/core/MUI/icons/icons';

interface StageMatchesByRoundProps {
  stageId: GUID;
  emptyMessage?: string;
}

interface TeamSideProps {
  team: IMatchResponse['homeTeam'];
}

function TeamSide({ team }: TeamSideProps) {
  return (
    <Stack direction="row" spacing={1} sx={{ alignItems: 'center', minWidth: 0 }}>
      <TeamLogo teamName={team?.name ?? '—'} logoUrl={team?.logoUrl} size={24} />
      <Typography variant="body2" noWrap>
        {team?.name ?? '—'}
      </Typography>
    </Stack>
  );
}

/**
 * The admin fixture for a single stage, grouped by matchday (jornada, HU-63):
 * "Fecha 1 / Partido 1..2, Fecha 2 / …". Uses the by-round endpoint so the
 * grouping key is the round, never the calendar date; each match keeps its own
 * date/time. Each match exposes a "Suspender / Reprogramar" action (HU-68) that
 * marks it suspended and optionally moves its date, without ever changing its
 * jornada (HU-67 — there is no change-round control). The team free that
 * matchday is shown as "Libre" (HU-65).
 */
export default function StageMatchesByRound({
  stageId,
  emptyMessage = 'No hay partidos cargados para esta fase.',
}: StageMatchesByRoundProps) {
  const navigate = useNavigate();
  const { getStageMatchesByRound, suspendMatch } = useMatch();
  const [rounds, setRounds] = useState<IRoundMatchesResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [target, setTarget] = useState<IMatchResponse | null>(null);
  const [newDate, setNewDate] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const reload = useCallback(async () => {
    setLoading(true);
    const response = await getStageMatchesByRound(stageId);
    setRounds(response ?? []);
    setLoading(false);
  }, [getStageMatchesByRound, stageId]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const stageTeamNames = useMemo(
    () => collectStageTeamNames(rounds.flatMap(round => round.matches)),
    [rounds]
  );

  const openSuspend = useCallback((match: IMatchResponse) => {
    setTarget(match);
    setNewDate('');
  }, []);

  const closeSuspend = useCallback(() => {
    setTarget(null);
    setNewDate('');
  }, []);

  const handleConfirmSuspend = useCallback(async () => {
    if (!target) {
      return;
    }

    setSubmitting(true);
    const result = await suspendMatch(target.id, {
      matchDate: newDate ? new Date(newDate).toISOString() : undefined,
    });
    setSubmitting(false);

    if (result) {
      closeSuspend();
      await reload();
    }
  }, [target, newDate, suspendMatch, closeSuspend, reload]);

  if (loading) {
    return <ListSkeleton items={4} />;
  }

  if (rounds.length === 0) {
    return <Typography sx={{ color: 'text.secondary' }}>{emptyMessage}</Typography>;
  }

  return (
    <Stack spacing={2.5}>
      {rounds.map(round => {
        const byes = byeTeamNamesForRound(round.matches, stageTeamNames);

        return (
          <Box key={round.round ?? 'knockout'}>
            <Typography
              variant="overline"
              sx={{ color: 'text.secondary', display: 'block', mb: 1 }}
            >
              {formatRoundLabel(round.round)}
            </Typography>
            <Paper variant="outlined">
              <Stack divider={<Divider />}>
                {round.matches.map(match => (
                  <Box
                    key={match.id}
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: {
                        xs: '1fr',
                        md: '150px 1fr auto 1fr auto auto',
                      },
                      alignItems: 'center',
                      gap: { xs: 1, md: 2 },
                      px: 2,
                      py: 1.25,
                    }}
                  >
                    <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                      {formatDateTimeAr(match.matchDate)}
                    </Typography>
                    <TeamSide team={match.homeTeam} />
                    <Typography
                      variant="body2"
                      sx={{ fontWeight: 'bold', textAlign: 'center', minWidth: 48 }}
                    >
                      {match.isFinished
                        ? formatMatchScore(
                            match.homeTeam?.score ?? 0,
                            match.visitorTeam?.score ?? 0
                          )
                        : 'vs'}
                    </Typography>
                    <TeamSide team={match.visitorTeam} />
                    <MatchStatusChip status={match.status} isFinished={match.isFinished} />
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={1}
                    >
                      <Button
                        size="small"
                        variant="contained"
                        onClick={() =>
                          navigate(APP_ROUTES.panelMatch.build(match.slug))
                        }
                      >
                        Cargar resultado
                      </Button>
                      <Button
                        size="small"
                        variant="outlined"
                        color="warning"
                        startIcon={<EventBusyIcon fontSize="small" />}
                        onClick={() => openSuspend(match)}
                      >
                        Suspender / Reprogramar
                      </Button>
                    </Stack>
                  </Box>
                ))}
                {byes.map(teamName => (
                  <Stack
                    key={`bye-${teamName}`}
                    direction="row"
                    spacing={1}
                    sx={{ alignItems: 'center', px: 2, py: 1.25 }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {teamName}
                    </Typography>
                    <Chip label={BYE_TEAM_LABEL} size="small" variant="outlined" />
                  </Stack>
                ))}
              </Stack>
            </Paper>
          </Box>
        );
      })}

      <Dialog open={Boolean(target)} onClose={closeSuspend} fullWidth maxWidth="xs">
        <DialogTitle>Suspender / Reprogramar partido</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              El partido se marcará como suspendido. Si indicás una nueva fecha,
              se reprograma sin cambiar su jornada.
            </Typography>
            <TextField
              label="Nueva fecha y hora"
              type="datetime-local"
              value={newDate}
              onChange={event => setNewDate(event.target.value)}
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeSuspend} disabled={submitting}>
            Cancelar
          </Button>
          <Button
            variant="contained"
            color="warning"
            onClick={() => void handleConfirmSuspend()}
            disabled={submitting}
          >
            Confirmar
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
