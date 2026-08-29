import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
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
import { usePlayer } from '@/modules/player/hook/player.hook';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PlayerStatisticCreatePage from '@/views/playerStatistic/playerStatisticCreatePage';
import PlayerStatisticCard from '@/views/playerStatistic/PlayerStatisticCard';
import PlayerHistory from '@/views/playerStatistic/PlayerHistory';
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

const PlayerPage: React.FC = () => {
  const { playerId } = useParams<{ playerId: string }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { player, getPlayerById } = usePlayer();
  const {
    playerStatistics,
    getPlayerStatisticsByFilter,
    playerCard,
    getPlayerCard,
    playerHistory,
    getPlayerHistory,
  } = usePlayerStatistic();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'ficha' | 'historial' | 'puntuaciones' | 'sanciones'
  >('detalle');
  const [statisticDialogOpen, setStatisticDialogOpen] = useState(false);
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);

  const targetPlayerId = useMemo(
    () => playerId ?? player?.id,
    [player?.id, playerId]
  );
  const isAdministrative = role !== UserRolesType.Guest;

  // Statistics and sanctions are filtered by the real player GUID, so they
  // only load once the slug-or-id route param has resolved to a fetched
  // player (player.id), never from the raw param which may be a slug.
  const refreshStatistics = () => {
    if (!player?.id) return;
    void getPlayerStatisticsByFilter({ playerId: player.id, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  const refreshSanctions = () => {
    if (!player?.id) return;
    void getPlayerSanctionByFilter({ playerId: player.id, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  const refreshCard = () => {
    if (!player?.id) return;
    void getPlayerCard(player.id);
  };

  const refreshHistory = () => {
    if (!player?.id) return;
    void getPlayerHistory(player.id);
  };

  useEffect(() => {
    if (tab === 'puntuaciones') {
      refreshStatistics();
    } else if (tab === 'sanciones') {
      refreshSanctions();
    } else if (tab === 'ficha') {
      refreshCard();
    } else if (tab === 'historial') {
      refreshHistory();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, player?.id]);

  useEffect(() => {
    if (!targetPlayerId) {
      return;
    }

    const fetchPlayer = async () => {
      setLoading(true);
      await getPlayerById(targetPlayerId, isAdministrative);
      setLoading(false);
    };

    void fetchPlayer();
  }, [getPlayerById, isAdministrative, targetPlayerId]);

  if (!targetPlayerId) {
    return (
      <PageShell title="Jugador">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió un jugador para visualizar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="Jugador">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (
    !player ||
    (player.id !== targetPlayerId && player.slug !== targetPlayerId)
  ) {
    return (
      <PageShell
        title="Jugador no encontrado"
        back={{
          label: 'Volver al listado',
          onClick: () => navigate(APP_ROUTES.panelPlayers),
        }}
      >
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información del jugador.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={player.fullName}
      back={{
        label: 'Volver al listado',
        onClick: () => navigate(APP_ROUTES.panelPlayers),
      }}
    >
        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Ficha" value="ficha" />
          <Tab label="Historial" value="historial" />
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
                Nombre completo
              </Typography>
              <Typography>{player.fullName}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Documento
              </Typography>
              <Typography>{player.documentNumber}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Fecha de nacimiento
              </Typography>
              <Typography>
                {player.birthDate
                  ? new Date(player.birthDate).toLocaleDateString('es-AR')
                  : '—'}
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
                Teléfono
              </Typography>
              <Typography>{player.phoneNumber || '—'}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Obra social
              </Typography>
              <Typography>{player.socialSecurity || '—'}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Estado de equipo
              </Typography>
              <Typography>
                {player.teamId ? 'Asignado a un equipo' : 'Sin equipo asignado'}
              </Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'ficha' && <PlayerStatisticCard card={playerCard} />}

        {tab === 'historial' && <PlayerHistory history={playerHistory} />}

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
                      primary={STATISTIC_TYPE_LABELS[statistic.type] ?? statistic.type}
                      secondary={formatDate(statistic.matchDate)}
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Typography variant="body2" sx={{
                color: "text.secondary"
              }}>
                Este jugador todavía no tiene puntuaciones registradas.
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
                      primary={sanction.description}
                      secondary={formatDate(sanction.issuedDate)}
                    />
                  </ListItem>
                ))}
              </List>
            ) : (
              <Typography variant="body2" sx={{
                color: "text.secondary"
              }}>
                Este jugador todavía no tiene sanciones registradas.
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
    </PageShell>
  );
};

export default PlayerPage;
