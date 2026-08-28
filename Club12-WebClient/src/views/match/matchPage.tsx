import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  IconButton,
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionResponse } from '@/modules/playerSanction/type/playerSanction.d';
import {
  formatFechasRemaining,
  formatSanctionDurationFechas,
  getSanctionStateLabel,
  getSanctionSubjectName,
  getSanctionSubjectTypeLabel,
} from '@/modules/playerSanction/utils/sanctionDisplay';
import {
  formatMatchDateToString,
  formatLongDateTimeAr,
} from '@/modules/core/utils/formatDate';
import {
  confirmAction,
  notifySuccess,
  notifyWarning,
} from '@/modules/core/utils/confirmDialog';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchStatisticsTab from '@/views/match/MatchStatisticsTab';
import MatchStatusChip from '@/views/match/MatchStatusChip';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PlayerSanctionCreatePage from '@/views/playerSanction/playerSanctionCreatePage';
import { VisibilityIcon } from '@/views/core/MUI/icons/icons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const formatDateTime = (value?: string | null) => formatLongDateTimeAr(value);

type MatchTab = 'detalle' | 'puntuaciones' | 'sanciones';

const MatchPage: React.FC = () => {
  const { matchId } = useParams<{ matchId: GUID }>();
  const navigate = useNavigate();
  const { match, getMatchById, putMatchScoreByMatchId, loadWalkOver } =
    useMatch();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [sanctionsLoading, setSanctionsLoading] = useState(false);
  const [tab, setTab] = useState<MatchTab>('detalle');
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);
  const [resultDialogOpen, setResultDialogOpen] = useState(false);
  const [walkoverDialogOpen, setWalkoverDialogOpen] = useState(false);
  const [submittingResult, setSubmittingResult] = useState(false);
  const [homeScore, setHomeScore] = useState('0');
  const [visitorScore, setVisitorScore] = useState('0');
  const [presentTeamId, setPresentTeamId] = useState<GUID | ''>('');

  const targetMatchId = useMemo(
    () => matchId ?? match?.id,
    [match?.id, matchId]
  );

  useEffect(() => {
    if (!targetMatchId) {
      return;
    }

    const fetchMatch = async () => {
      setLoading(true);
      await getMatchById(targetMatchId);
      setLoading(false);
    };

    void fetchMatch();
  }, [getMatchById, targetMatchId]);

  // Sanctions are filtered server-side by a real GUID MatchId — unlike the
  // match fetch itself, this can never accept the route's idOrSlug value,
  // so it must wait for `match` to resolve rather than using targetMatchId.
  const refreshSanctions = useCallback(() => {
    if (!match?.id) {
      return;
    }

    setSanctionsLoading(true);
    void getPlayerSanctionByFilter({ matchId: match.id }).finally(() => {
      setSanctionsLoading(false);
    });
  }, [getPlayerSanctionByFilter, match?.id]);

  useEffect(() => {
    if (tab !== 'sanciones' || !match?.id) {
      return;
    }

    refreshSanctions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, match?.id]);

  const openResultDialog = useCallback(() => {
    setHomeScore(String(match?.homeTeam?.score ?? 0));
    setVisitorScore(String(match?.visitorTeam?.score ?? 0));
    setResultDialogOpen(true);
  }, [match?.homeTeam?.score, match?.visitorTeam?.score]);

  const handleLoadResult = useCallback(async () => {
    if (!match?.id) {
      return;
    }

    const home = Number(homeScore);
    const visitor = Number(visitorScore);

    if (Number.isNaN(home) || Number.isNaN(visitor)) {
      await notifyWarning({ title: 'Ingrese un marcador válido.' });
      return;
    }

    // Mirror the backend no-draws rule (HU-70) client-side so the operator
    // gets an immediate, clear message instead of a round-trip 409.
    if (home === visitor) {
      await notifyWarning({
        title: 'No se permiten empates',
        text: 'En básquet todo partido cargado debe tener un ganador.',
      });
      return;
    }

    setSubmittingResult(true);
    const result = await putMatchScoreByMatchId(match.id, {
      homeScore: home,
      visitorScore: visitor,
    });
    setSubmittingResult(false);

    if (!result) {
      return;
    }

    setResultDialogOpen(false);
    await notifySuccess({ title: 'Resultado cargado' });
  }, [homeScore, visitorScore, match?.id, putMatchScoreByMatchId]);

  const openWalkoverDialog = useCallback(() => {
    setPresentTeamId(match?.homeTeam?.id ?? match?.visitorTeam?.id ?? '');
    setWalkoverDialogOpen(true);
  }, [match?.homeTeam?.id, match?.visitorTeam?.id]);

  const handleWalkover = useCallback(async () => {
    if (!match?.id || !presentTeamId) {
      return;
    }

    const presentName =
      presentTeamId === match.homeTeam?.id
        ? match.homeTeam?.name
        : match.visitorTeam?.name;

    const confirmed = await confirmAction({
      title: '¿Marcar walkover?',
      text: `Se otorgará el resultado reglamentario a ${presentName ?? 'el equipo presente'}.`,
      icon: 'warning',
    });

    if (!confirmed) {
      return;
    }

    setSubmittingResult(true);
    const result = await loadWalkOver(match.id, { presentTeamId });
    setSubmittingResult(false);

    if (!result) {
      return;
    }

    setWalkoverDialogOpen(false);
    await notifySuccess({ title: 'Walkover cargado' });
  }, [match, presentTeamId, loadWalkOver]);

  if (!targetMatchId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Partido</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió un partido para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!match || (match.id !== targetMatchId && match.slug !== targetMatchId)) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Partido no encontrado</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información del partido.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate(APP_ROUTES.panelMatches)}
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

  const homeTeam = match.homeTeam;
  const visitorTeam = match.visitorTeam;
  const sanctions = playerSanctions ?? [];
  const winningTeamName =
    match.winningTeamId === homeTeam?.id
      ? homeTeam.name
      : match.winningTeamId === visitorTeam?.id
        ? visitorTeam.name
        : '—';

  const renderTeamName = (
    team: typeof homeTeam,
    fallbackLabel: string,
    roleLabel: string
  ) => (
    <Stack spacing={0.5} sx={{
      alignItems: "center"
    }}>
      <Typography variant="body2" sx={{
        color: "text.secondary"
      }}>
        {roleLabel}
      </Typography>
      <Stack direction="row" spacing={0.5} sx={{
        alignItems: "center"
      }}>
        <Typography variant="subtitle1">
          {team?.name || fallbackLabel}
        </Typography>
        {team?.id && (
          <Tooltip title="Ver equipo">
            <IconButton
              size="small"
              color="primary"
              onClick={() => navigate(APP_ROUTES.panelTeamDetail.build(team.id))}
            >
              <VisibilityIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )}
      </Stack>
    </Stack>
  );

  return (
    <Card>
      <CardContent>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={2}
          sx={{
            alignItems: { xs: 'flex-start', sm: 'center' },
            justifyContent: "space-between",
            mb: 3
          }}>
          <Typography variant="h6">Partido</Typography>
          <Button
            variant="contained"
            color="primary"
            onClick={() => navigate(APP_ROUTES.panelMatches)}
          >
            Volver
          </Button>
        </Stack>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
          variant="scrollable"
          scrollButtons="auto"
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Puntuaciones" value="puntuaciones" />
          <Tab label="Sanciones" value="sanciones" />
        </Tabs>

        {tab === 'detalle' && (
          <>
            <Stack
              direction="row"
              spacing={1}
              sx={{ justifyContent: 'flex-end', flexWrap: 'wrap', mb: 2 }}
            >
              <Button variant="contained" onClick={openResultDialog}>
                Cargar resultado
              </Button>
              <Button
                variant="outlined"
                color="warning"
                onClick={openWalkoverDialog}
                disabled={!homeTeam || !visitorTeam}
              >
                Marcar W.O.
              </Button>
            </Stack>

            <Stack
              direction={{ xs: 'column', md: 'row' }}
              spacing={3}
              sx={{
                justifyContent: "space-between",
                alignItems: "center",
                mb: 3
              }}>
              <Stack
                spacing={1}
                sx={{
                  alignItems: "center",
                  minWidth: 180
                }}>
                <TeamLogo
                  teamName={homeTeam?.name || 'Local'}
                  logoUrl={homeTeam?.logoUrl}
                  size={56}
                />
                {renderTeamName(homeTeam, '—', 'Local')}
                <Typography variant="h4">{homeTeam?.score ?? '—'}</Typography>
              </Stack>

              <Typography variant="h5" sx={{
                color: "text.secondary"
              }}>
                VS
              </Typography>

              <Stack
                spacing={1}
                sx={{
                  alignItems: "center",
                  minWidth: 180
                }}>
                <TeamLogo
                  teamName={visitorTeam?.name || 'Visitante'}
                  logoUrl={visitorTeam?.logoUrl}
                  size={56}
                />
                {renderTeamName(visitorTeam, '—', 'Visitante')}
                <Typography variant="h4">
                  {visitorTeam?.score ?? '—'}
                </Typography>
              </Stack>
            </Stack>

            <Grid container spacing={2}>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Fecha
                </Typography>
                <Typography>{formatDateTime(match.matchDate)}</Typography>
              </Grid>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Tipo
                </Typography>
                <Typography>{match.matchType}</Typography>
              </Grid>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Estado
                </Typography>
                <Box sx={{ mt: 0.5 }}>
                  <MatchStatusChip
                    status={match.status}
                    isFinished={match.isFinished}
                  />
                </Box>
              </Grid>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Ganador
                </Typography>
                <Typography>{winningTeamName}</Typography>
              </Grid>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Cancha
                </Typography>
                <Typography>{match.venue?.name || '—'}</Typography>
              </Grid>
              <Grid
                size={{
                  xs: 12,
                  md: 6
                }}>
                <Typography variant="subtitle2" sx={{
                  color: "text.secondary"
                }}>
                  Dirección
                </Typography>
                <Typography>{match.venue?.address || '—'}</Typography>
              </Grid>
            </Grid>
          </>
        )}

        {tab === 'puntuaciones' && <MatchStatisticsTab match={match} />}

        {tab === 'sanciones' && (
          <>
            <Stack direction="row" sx={{
              justifyContent: "flex-end",
              mb: 2
            }}>
              <NewEntityButton
                type="Sanción"
                onClick={() => setSanctionDialogOpen(true)}
              />
            </Stack>

            {sanctionsLoading ? (
              <LoadingIndicator />
            ) : sanctions.length > 0 ? (
              <Grid container spacing={2}>
                {sanctions.map((sanction: IPlayerSanctionResponse) => (
                  <Grid
                    key={sanction.id}
                    size={{
                      xs: 12,
                      md: 6
                    }}>
                    <Card variant="outlined">
                      <CardContent>
                        <Stack spacing={1.5}>
                          <Typography variant="h6">
                            {getSanctionSubjectName(sanction)}
                          </Typography>
                          <Typography variant="body2" sx={{
                            color: "text.secondary"
                          }}>
                            {getSanctionSubjectTypeLabel(sanction)} ·{' '}
                            {getSanctionStateLabel(sanction)}
                          </Typography>
                          <Typography variant="body2" sx={{
                            color: "text.secondary"
                          }}>
                            Duración: {formatSanctionDurationFechas(sanction.duration)}
                            {' · Restantes: '}
                            {formatFechasRemaining(sanction.fechasRemaining)}
                          </Typography>
                          <Typography variant="body2" sx={{
                            color: "text.secondary"
                          }}>
                            Fecha:{' '}
                            {formatMatchDateToString(
                              new Date(sanction.issuedDate).toISOString()
                            )}
                          </Typography>
                          <Typography variant="body2">
                            {sanction.description}
                          </Typography>
                          {sanction.subjectType === 'Player' &&
                            sanction.playerId && (
                              <Box>
                                <Button
                                  variant="text"
                                  onClick={() =>
                                    navigate(
                                      APP_ROUTES.panelPlayer.build(
                                        sanction.playerId as GUID
                                      )
                                    )
                                  }
                                >
                                  Ver jugador
                                </Button>
                              </Box>
                            )}
                        </Stack>
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            ) : (
              <Typography variant="body2" sx={{
                color: "text.secondary"
              }}>
                Este partido no tiene sanciones registradas.
              </Typography>
            )}
          </>
        )}
      </CardContent>

      <PlayerSanctionCreatePage
        open={sanctionDialogOpen}
        onClose={() => setSanctionDialogOpen(false)}
        onCreated={refreshSanctions}
        presetMatch={match}
      />

      <Dialog
        open={resultDialogOpen}
        onClose={() => !submittingResult && setResultDialogOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Cargar resultado</DialogTitle>
        <DialogContent>
          <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
            <TextField
              type="number"
              label={homeTeam?.name || 'Local'}
              value={homeScore}
              onChange={e => setHomeScore(e.target.value)}
              slotProps={{ htmlInput: { min: 0 } }}
              fullWidth
            />
            <TextField
              type="number"
              label={visitorTeam?.name || 'Visitante'}
              value={visitorScore}
              onChange={e => setVisitorScore(e.target.value)}
              slotProps={{ htmlInput: { min: 0 } }}
              fullWidth
            />
          </Stack>
          <Typography variant="caption" sx={{ color: 'text.secondary', mt: 1, display: 'block' }}>
            No se permiten empates: el partido debe tener un ganador.
          </Typography>
          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end', mt: 2 }}>
            <Button
              color="inherit"
              onClick={() => setResultDialogOpen(false)}
              disabled={submittingResult}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void handleLoadResult()}
              disabled={submittingResult || homeScore === visitorScore}
            >
              Guardar
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>

      <Dialog
        open={walkoverDialogOpen}
        onClose={() => !submittingResult && setWalkoverDialogOpen(false)}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle>Marcar walkover (W.O.)</DialogTitle>
        <DialogContent>
          <Typography variant="body2" sx={{ color: 'text.secondary', mb: 2 }}>
            Seleccione el equipo que se presentó. Recibirá el resultado
            reglamentario por defecto.
          </Typography>
          <TextField
            select
            label="Equipo presente"
            value={presentTeamId}
            onChange={e => setPresentTeamId(e.target.value as GUID)}
            fullWidth
          >
            {homeTeam && (
              <MenuItem value={homeTeam.id}>{homeTeam.name}</MenuItem>
            )}
            {visitorTeam && (
              <MenuItem value={visitorTeam.id}>{visitorTeam.name}</MenuItem>
            )}
          </TextField>
          <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end', mt: 2 }}>
            <Button
              color="inherit"
              onClick={() => setWalkoverDialogOpen(false)}
              disabled={submittingResult}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              color="warning"
              onClick={() => void handleWalkover()}
              disabled={submittingResult || !presentTeamId}
            >
              Confirmar W.O.
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Card>
  );
};

export default MatchPage;
