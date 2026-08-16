import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Card,
  CardContent,
  Grid,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import { notifyInfo } from '@/modules/core/utils/confirmDialog';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const PlayerPage: React.FC = () => {
  const { playerId } = useParams<{ playerId: GUID }>();
  const navigate = useNavigate();
  const { role } = useAuth();
  const { player, getPlayerById } = usePlayer();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'puntuaciones' | 'sanciones'>(
    'detalle'
  );

  const targetPlayerId = useMemo(
    () => playerId ?? player?.id,
    [player?.id, playerId]
  );
  const isAdministrative = role !== UserRolesType.Guest;

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
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
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
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
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

  const handleCreateScore = () => {
    void notifyInfo({
      title: 'Pendiente',
      text: 'La creación de puntuaciones desde esta vista aún no está implementada.',
    });
  };

  const handleCreateSanction = () => {
    void notifyInfo({
      title: 'Pendiente',
      text: 'La creación de sanciones desde esta vista aún no está implementada.',
    });
  };

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" mb={2}>
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
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Nombre completo
              </Typography>
              <Typography>{player.fullName}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Documento
              </Typography>
              <Typography>{player.documentNumber}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Fecha de nacimiento
              </Typography>
              <Typography>
                {player.birthDate
                  ? new Date(player.birthDate).toLocaleDateString('es-AR')
                  : '—'}
              </Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Teléfono
              </Typography>
              <Typography>{player.phoneNumber || '—'}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Obra social
              </Typography>
              <Typography>{player.socialSecurity || '—'}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
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
            <Stack direction="row" justifyContent="flex-end" mb={2}>
              <NewEntityButton type="Puntuación" onClick={handleCreateScore} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              Próximamente: puntuaciones del jugador.
            </Typography>
          </>
        )}

        {tab === 'sanciones' && (
          <>
            <Stack direction="row" justifyContent="flex-end" mb={2}>
              <NewEntityButton type="Sanción" onClick={handleCreateSanction} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              Próximamente: sanciones del jugador.
            </Typography>
          </>
        )}
      </CardContent>
    </Card>
  );
};

export default PlayerPage;
