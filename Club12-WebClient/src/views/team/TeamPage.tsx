import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Card,
  CardContent,
  Chip,
  Grid,
  List,
  ListItem,
  ListItemText,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import PlayersPage from '@/views/player/PlayersPage';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PlayerStatisticCreatePage from '@/views/playerStatistic/playerStatisticCreatePage';
import PlayerSanctionCreatePage from '@/views/playerSanction/playerSanctionCreatePage';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { STATISTIC_TYPE_LABELS } from '@/modules/playerStatistic/utils/playerStatisticDisplay';

const formatDate = (value?: string | Date | null) => {
  if (!value) {
    return '—';
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString('es-AR');
};

interface TeamPageProps {
  teamIdOverride?: GUID;
  wrapInCard?: boolean;
  hideBackLink?: boolean;
}

const TeamPage: React.FC<TeamPageProps> = ({
  teamIdOverride,
  wrapInCard = true,
  hideBackLink = false,
}) => {
  const { teamId } = useParams<{ teamId: GUID }>();
  const navigate = useNavigate();
  const { team, getTeamById } = useTeam();
  const { playerStatistics, getPlayerStatisticsByFilter } = usePlayerStatistic();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'jugadores' | 'puntuaciones' | 'sanciones'
  >('detalle');
  const [statisticDialogOpen, setStatisticDialogOpen] = useState(false);
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);

  const targetTeamId = useMemo(
    () => teamIdOverride ?? teamId ?? team?.id,
    [team?.id, teamId, teamIdOverride]
  );

  useEffect(() => {
    if (!targetTeamId) {
      return;
    }

    const fetchTeam = async () => {
      setLoading(true);
      await getTeamById(targetTeamId);
      setLoading(false);
    };

    void fetchTeam();
  }, [getTeamById, targetTeamId]);

  const refreshStatistics = () => {
    if (!targetTeamId) return;
    void getPlayerStatisticsByFilter({ teamId: targetTeamId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  const refreshSanctions = () => {
    if (!targetTeamId) return;
    void getPlayerSanctionByFilter({ teamId: targetTeamId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  useEffect(() => {
    if (tab === 'puntuaciones') {
      refreshStatistics();
    } else if (tab === 'sanciones') {
      refreshSanctions();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, targetTeamId]);

  const playerNameById = useMemo(() => {
    const map = new Map<GUID, string>();
    (team?.players ?? []).forEach(player => map.set(player.id, player.fullName));
    return map;
  }, [team?.players]);

  if (!targetTeamId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Equipo</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió un equipo para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!team || (team.id !== targetTeamId && team.slug !== targetTeamId)) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Equipo no encontrado</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información del equipo.
          </Typography>
          {!hideBackLink && (
            <Typography
              component="button"
              onClick={() => navigate(APP_ROUTES.panelTeams)}
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
          )}
        </CardContent>
      </Card>
    );
  }

  const content = (
    <>
      <Stack
        direction="row"
        spacing={1.5}
        sx={{
          alignItems: "center",
          mb: 2
        }}>
        <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={44} />
        <Typography variant="h6">{team.name}</Typography>
      </Stack>

      <Tabs
        value={tab}
        onChange={(_, value) => setTab(value)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
      >
        <Tab label="Detalle" value="detalle" />
        <Tab label="Jugadores" value="jugadores" />
        <Tab label="Puntuaciones" value="puntuaciones" />
        <Tab label="Sanciones" value="sanciones" />
      </Tabs>

      {tab === 'detalle' && (
        <Grid container spacing={2}>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <Typography variant="subtitle2" sx={{
              color: "text.secondary"
            }}>
              Nombre
            </Typography>
            <Typography>{team.name}</Typography>
          </Grid>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <Typography variant="subtitle2" sx={{
              color: "text.secondary"
            }}>
              Código
            </Typography>
            <Typography>{team.threeLetterCode}</Typography>
          </Grid>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <Typography variant="subtitle2" sx={{
              color: "text.secondary"
            }}>
              Color camiseta
            </Typography>
            <Typography>{team.shirtColor || '—'}</Typography>
          </Grid>
          <Grid
            size={{
              xs: 12,
              md: 6
            }}>
            <Typography variant="subtitle2" sx={{
              color: "text.secondary"
            }}>
              Jugadores registrados
            </Typography>
            <Typography>{team.players?.length ?? 0}</Typography>
          </Grid>
        </Grid>
      )}

      {tab === 'jugadores' && (
        <PlayersPage
          teamId={targetTeamId}
          title={undefined}
          emptyMessage="Este equipo no tiene jugadores cargados."
          wrapInCard={false}
        />
      )}

      {tab === 'puntuaciones' && (
        <>
          <Stack
            direction="row"
            sx={{
              justifyContent: "flex-end",
              mb: 2
            }}>
            <NewEntityButton
              type="Puntuación"
              onClick={() => setStatisticDialogOpen(true)}
            />
          </Stack>
          {playerStatistics && playerStatistics.length > 0 ? (
            <List disablePadding>
              {playerStatistics.map(statistic => (
                <ListItem
                  key={statistic.id}
                  divider
                  secondaryAction={<Chip size="small" label={statistic.value} />}
                >
                  <ListItemText
                    primary={playerNameById.get(statistic.playerId) ?? '—'}
                    secondary={`${STATISTIC_TYPE_LABELS[statistic.type] ?? statistic.type} · ${formatDate(statistic.matchDate)}`}
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>
              Este equipo todavía no tiene puntuaciones registradas.
            </Typography>
          )}
        </>
      )}

      {tab === 'sanciones' && (
        <>
          <Stack
            direction="row"
            sx={{
              justifyContent: "flex-end",
              mb: 2
            }}>
            <NewEntityButton
              type="Sanción"
              onClick={() => setSanctionDialogOpen(true)}
            />
          </Stack>
          {playerSanctions && playerSanctions.length > 0 ? (
            <List disablePadding>
              {playerSanctions.map(sanction => (
                <ListItem
                  key={sanction.id}
                  divider
                  secondaryAction={<Chip size="small" label={`${sanction.duration} partidos`} />}
                >
                  <ListItemText
                    primary={sanction.playerFullName}
                    secondary={`${sanction.description} · ${formatDate(sanction.issuedDate)}`}
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>
              Este equipo todavía no tiene sanciones registradas.
            </Typography>
          )}
        </>
      )}

      <PlayerStatisticCreatePage
        open={statisticDialogOpen}
        onClose={() => setStatisticDialogOpen(false)}
        onCreated={refreshStatistics}
      />
      <PlayerSanctionCreatePage
        open={sanctionDialogOpen}
        onClose={() => setSanctionDialogOpen(false)}
        onCreated={refreshSanctions}
      />
    </>
  );

  if (wrapInCard) {
    return (
      <Card>
        <CardContent>{content}</CardContent>
      </Card>
    );
  }

  return content;
};

export default TeamPage;
