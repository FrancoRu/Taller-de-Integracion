import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Card, CardContent, Grid, Tab, Tabs, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';

const formatDate = (value?: string | Date | null) => {
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

const PlayerSanctionPage: React.FC = () => {
  const { playerSanctionId } = useParams<{ playerSanctionId: GUID }>();
  const navigate = useNavigate();
  const { playerSanction, getPlayerSanctionById } = usePlayerSanction();
  const { player, getPlayerById } = usePlayer();
  const { match, getMatchById } = useMatch();
  const [loading, setLoading] = useState(false);
  const [playerLoading, setPlayerLoading] = useState(false);
  const [matchLoading, setMatchLoading] = useState(false);
  const [tab, setTab] = useState<'detalle' | 'jugador' | 'partido'>('detalle');

  const targetSanctionId = useMemo(
    () => playerSanctionId ?? playerSanction?.id,
    [playerSanction?.id, playerSanctionId]
  );

  useEffect(() => {
    if (!targetSanctionId) {
      return;
    }

    const fetchSanction = async () => {
      setLoading(true);
      await getPlayerSanctionById(targetSanctionId);
      setLoading(false);
    };

    void fetchSanction();
  }, [getPlayerSanctionById, targetSanctionId]);

  useEffect(() => {
    if (tab !== 'jugador' || !playerSanction?.playerId) {
      return;
    }

    const fetchPlayer = async () => {
      setPlayerLoading(true);
      await getPlayerById(playerSanction.playerId, true);
      setPlayerLoading(false);
    };

    void fetchPlayer();
  }, [getPlayerById, playerSanction?.playerId, tab]);

  useEffect(() => {
    if (tab !== 'partido' || !playerSanction?.matchId) {
      return;
    }

    if (match?.id === playerSanction.matchId) {
      return;
    }

    const fetchMatch = async () => {
      setMatchLoading(true);
      await getMatchById(playerSanction.matchId);
      setMatchLoading(false);
    };

    void fetchMatch();
  }, [getMatchById, match?.id, playerSanction?.matchId, tab]);

  if (!targetSanctionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Sanción</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No se recibió una sanción para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!playerSanction || playerSanction.id !== targetSanctionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Sanción no encontrada</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            No fue posible cargar la información de la sanción.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate('/panel/sanciones')}
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
        <Typography variant="h6" mb={2}>
          Sanción
        </Typography>

        <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Jugador" value="jugador" />
          <Tab label="Partido" value="partido" />
        </Tabs>

        {tab === 'detalle' && (
          <Grid container spacing={2}>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Duración
              </Typography>
              <Typography>{playerSanction.duration}</Typography>
            </Grid>
            <Grid item xs={12} md={6}>
              <Typography variant="subtitle2" color="text.secondary">
                Fecha de emisión
              </Typography>
              <Typography>{formatDate(playerSanction.issuedDate)}</Typography>
            </Grid>
            <Grid item xs={12}>
              <Typography variant="subtitle2" color="text.secondary">
                Descripción
              </Typography>
              <Typography>{playerSanction.description || '—'}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'jugador' && (
          <>
            {playerLoading ? (
              <LoadingIndicator />
            ) : !player || player.id !== playerSanction.playerId ? (
              <Typography variant="body2" color="text.secondary">
                No fue posible cargar la información del jugador.
              </Typography>
            ) : (
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
              </Grid>
            )}
          </>
        )}

        {tab === 'partido' && (
          <>
            {matchLoading ? (
              <LoadingIndicator />
            ) : !match || match.id !== playerSanction.matchId ? (
              <Typography variant="body2" color="text.secondary">
                No fue posible cargar la información del partido.
              </Typography>
            ) : (
              <Grid container spacing={2}>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Fecha
                  </Typography>
                  <Typography>{formatDate(match.matchDate)}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Estado
                  </Typography>
                  <Typography>
                    {match.isFinished ? 'Finalizado' : 'Programado'}
                  </Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Equipo local
                  </Typography>
                  <Typography>{match.homeTeam?.name || '—'}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Equipo visitante
                  </Typography>
                  <Typography>{match.visitorTeam?.name || '—'}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Cancha
                  </Typography>
                  <Typography>{match.venue?.name || '—'}</Typography>
                </Grid>
              </Grid>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
};

export default PlayerSanctionPage;
