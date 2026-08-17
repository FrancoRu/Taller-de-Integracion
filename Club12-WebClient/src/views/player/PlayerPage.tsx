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
import { usePlayer } from '@/modules/player/hook/player.hook';
import { usePlayerStatistic } from '@/modules/playerStatistic/hook/playerStatistic.hook';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
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

const PlayerPage: React.FC = () => {
  const { playerId } = useParams<{ playerId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { player, getPlayerById } = usePlayer();
  const { playerStatistics, getPlayerStatisticsByFilter } = usePlayerStatistic();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'puntuaciones' | 'sanciones'>(
    'detalle'
  );
  const [statisticDialogOpen, setStatisticDialogOpen] = useState(false);
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);

  const targetPlayerId = useMemo(
    () => playerId ?? player?.id,
    [player?.id, playerId]
  );
  const isAdministrative = role !== UserRolesType.Guest;

  const refreshStatistics = () => {
    if (!targetPlayerId) return;
    void getPlayerStatisticsByFilter({ playerId: targetPlayerId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  const refreshSanctions = () => {
    if (!targetPlayerId) return;
    void getPlayerSanctionByFilter({ playerId: targetPlayerId, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  useEffect(() => {
    if (tab === 'puntuaciones') {
      refreshStatistics();
    } else if (tab === 'sanciones') {
      refreshSanctions();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, targetPlayerId]);

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
      <Card>
        <CardContent>
          <Typography variant="h6">Jugador</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió un jugador para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!player || player.id !== targetPlayerId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Jugador no encontrado</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información del jugador.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate(APP_ROUTES.panelPlayers)}
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

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 2
        }}>
          {player.fullName}
        </Typography>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
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
      </CardContent>

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
    </Card>
  );
};

export default PlayerPage;
