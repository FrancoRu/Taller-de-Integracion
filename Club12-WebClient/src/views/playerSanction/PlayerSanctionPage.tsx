import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material';
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';
import { usePlayerSanction } from '@/modules/playerSanction/hook/playerSanction.hook';
import { SanctionAppealStatus } from '@/modules/playerSanction/type/playerSanction';
import { usePlayer } from '@/modules/player/hook/player.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';

const APPEAL_STATUS_LABEL: Record<SanctionAppealStatus, string> = {
  None: 'Sin apelación',
  Pending: 'Apelación pendiente',
  Accepted: 'Apelación aceptada',
  Rejected: 'Apelación rechazada',
};

const APPEAL_STATUS_COLOR: Record<
  SanctionAppealStatus,
  'default' | 'warning' | 'success' | 'error'
> = {
  None: 'default',
  Pending: 'warning',
  Accepted: 'success',
  Rejected: 'error',
};

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
  const {
    playerSanction,
    getPlayerSanctionById,
    appealPlayerSanction,
    resolvePlayerSanctionAppeal,
  } = usePlayerSanction();
  const { player, getPlayerById } = usePlayer();
  const { match, getMatchById } = useMatch();
  const [loading, setLoading] = useState(false);
  const [playerLoading, setPlayerLoading] = useState(false);
  const [matchLoading, setMatchLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'jugador' | 'partido' | 'apelacion'
  >('detalle');
  const [appealDialogOpen, setAppealDialogOpen] = useState(false);
  const [resolveDialogOpen, setResolveDialogOpen] = useState(false);
  const [reasonText, setReasonText] = useState('');
  const [resolutionText, setResolutionText] = useState('');
  const [decisionAccepted, setDecisionAccepted] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  const submitAppeal = async () => {
    if (!playerSanction || !reasonText.trim()) {
      return;
    }
    setSubmitting(true);
    const result = await appealPlayerSanction(playerSanction.id, {
      reason: reasonText.trim(),
    });
    setSubmitting(false);
    if (result) {
      setAppealDialogOpen(false);
      setReasonText('');
      await Swal.fire({
        title: 'Apelación registrada',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
      });
    }
  };

  const submitResolution = async () => {
    if (!playerSanction || !resolutionText.trim()) {
      return;
    }
    setSubmitting(true);
    const result = await resolvePlayerSanctionAppeal(playerSanction.id, {
      accepted: decisionAccepted,
      resolution: resolutionText.trim(),
    });
    setSubmitting(false);
    if (result) {
      setResolveDialogOpen(false);
      setResolutionText('');
      await Swal.fire({
        title: 'Apelación resuelta',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
      });
    }
  };

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
          <Tab label="Apelación" value="apelacion" />
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

        {tab === 'apelacion' && (
          <Stack spacing={2}>
            <Stack
              direction="row"
              alignItems="center"
              justifyContent="space-between"
              flexWrap="wrap"
              gap={1}
            >
              <Chip
                label={APPEAL_STATUS_LABEL[playerSanction.appealStatus]}
                color={APPEAL_STATUS_COLOR[playerSanction.appealStatus]}
              />
              <Stack direction="row" spacing={1}>
                {playerSanction.appealStatus !== 'Pending' && (
                  <Button
                    variant="outlined"
                    onClick={() => {
                      setReasonText('');
                      setAppealDialogOpen(true);
                    }}
                  >
                    Registrar apelación
                  </Button>
                )}
                {playerSanction.appealStatus === 'Pending' && (
                  <Button
                    variant="contained"
                    onClick={() => {
                      setResolutionText('');
                      setDecisionAccepted(true);
                      setResolveDialogOpen(true);
                    }}
                  >
                    Resolver apelación
                  </Button>
                )}
              </Stack>
            </Stack>

            {playerSanction.appealStatus === 'None' ? (
              <Typography variant="body2" color="text.secondary">
                Esta sanción no tiene apelaciones registradas.
              </Typography>
            ) : (
              <Grid container spacing={2}>
                <Grid item xs={12}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Motivo de la apelación
                  </Typography>
                  <Typography>{playerSanction.appealReason || '—'}</Typography>
                </Grid>
                <Grid item xs={12} md={6}>
                  <Typography variant="subtitle2" color="text.secondary">
                    Fecha de apelación
                  </Typography>
                  <Typography>{formatDate(playerSanction.appealDate)}</Typography>
                </Grid>
                {playerSanction.appealResolution && (
                  <>
                    <Grid item xs={12}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Resolución (decisión registrada)
                      </Typography>
                      <Typography>{playerSanction.appealResolution}</Typography>
                    </Grid>
                    <Grid item xs={12} md={6}>
                      <Typography variant="subtitle2" color="text.secondary">
                        Fecha de resolución
                      </Typography>
                      <Typography>
                        {formatDate(playerSanction.appealResolvedDate)}
                      </Typography>
                    </Grid>
                  </>
                )}
              </Grid>
            )}
          </Stack>
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

      <Dialog
        open={appealDialogOpen}
        onClose={() => !submitting && setAppealDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Registrar apelación</DialogTitle>
        <DialogContent>
          <TextField
            label="Motivo de la apelación"
            value={reasonText}
            onChange={e => setReasonText(e.target.value)}
            multiline
            minRows={3}
            fullWidth
            autoFocus
            sx={{ mt: 1 }}
          />
          <Stack direction="row" justifyContent="flex-end" spacing={1} mt={2}>
            <Button
              color="inherit"
              onClick={() => setAppealDialogOpen(false)}
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void submitAppeal()}
              disabled={submitting || !reasonText.trim()}
            >
              Registrar
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>

      <Dialog
        open={resolveDialogOpen}
        onClose={() => !submitting && setResolveDialogOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Resolver apelación</DialogTitle>
        <DialogContent>
          <TextField
            select
            label="Decisión"
            value={decisionAccepted ? 'accepted' : 'rejected'}
            onChange={e => setDecisionAccepted(e.target.value === 'accepted')}
            fullWidth
            SelectProps={{ native: true }}
            sx={{ mt: 1 }}
          >
            <option value="accepted">Aceptar apelación</option>
            <option value="rejected">Rechazar apelación</option>
          </TextField>
          <TextField
            label="Resolución (decisión registrada)"
            value={resolutionText}
            onChange={e => setResolutionText(e.target.value)}
            multiline
            minRows={3}
            fullWidth
            sx={{ mt: 2 }}
          />
          <Stack direction="row" justifyContent="flex-end" spacing={1} mt={2}>
            <Button
              color="inherit"
              onClick={() => setResolveDialogOpen(false)}
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button
              variant="contained"
              onClick={() => void submitResolution()}
              disabled={submitting || !resolutionText.trim()}
            >
              Guardar decisión
            </Button>
          </Stack>
        </DialogContent>
      </Dialog>
    </Card>
  );
};

export default PlayerSanctionPage;
