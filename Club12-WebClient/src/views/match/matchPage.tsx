import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardContent,
  Grid,
  IconButton,
  Stack,
  Tab,
  Tabs,
  Tooltip,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useMatch } from '@/modules/match/hook/match.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { IPlayerSanctionResponse } from '@/modules/playerSanction/type/playerSanction.d';
import { formatMatchDateToString } from '@/modules/core/utils/formatDate';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchStatisticsTab from '@/views/match/MatchStatisticsTab';
import { VisibilityIcon } from '@/views/core/MUI/icons/icons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const formatDateTime = (value?: string | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return '—';
  }

  return parsed.toLocaleString('es-AR', {
    dateStyle: 'full',
    timeStyle: 'short',
  });
};

type MatchTab = 'detalle' | 'puntuaciones' | 'sanciones';

const MatchPage: React.FC = () => {
  const { matchId } = useParams<{ matchId: GUID }>();
  const navigate = useNavigate();
  const { match, getMatchById } = useMatch();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [sanctionsLoading, setSanctionsLoading] = useState(false);
  const [tab, setTab] = useState<MatchTab>('detalle');

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

  useEffect(() => {
    if (tab !== 'sanciones' || !targetMatchId) {
      return;
    }

    const fetchSanctions = async () => {
      setSanctionsLoading(true);
      await getPlayerSanctionByFilter({ matchId: targetMatchId });
      setSanctionsLoading(false);
    };

    void fetchSanctions();
  }, [getPlayerSanctionByFilter, tab, targetMatchId]);

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
        <Typography variant="h6" sx={{
          mb: 3
        }}>
          Partido
        </Typography>

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
                <Typography>
                  {match.isFinished ? 'Finalizado' : 'Programado'}
                </Typography>
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
                            {sanction.playerFullName}
                          </Typography>
                          <Typography variant="body2" sx={{
                            color: "text.secondary"
                          }}>
                            Duración: {sanction.duration} partidos
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
                          <Box>
                            <Button
                              variant="text"
                              onClick={() =>
                                navigate(
                                  APP_ROUTES.panelPlayer.build(sanction.playerId)
                                )
                              }
                            >
                              Ver jugador
                            </Button>
                          </Box>
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
    </Card>
  );
};

export default MatchPage;
