import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
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
import { useTeam } from '@/modules/team/hook/team.hook';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { GUID } from '@/modules/core/types/types';
import { IPutPlayerRequest } from '@/modules/player/type/player.d';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PlayerStatisticCreatePage from '@/views/playerStatistic/playerStatisticCreatePage';
import PlayerStatisticCard from '@/views/playerStatistic/PlayerStatisticCard';
import PlayerHistory from '@/views/playerStatistic/PlayerHistory';
import PlayerSanctionCreatePage from '@/views/playerSanction/playerSanctionCreatePage';
import PlayerFormDialog from '@/views/player/PlayerFormDialog';
import type { PlayerFormField, PlayerFormState } from '@/views/player/players.types';
import { toDateInputValue } from '@/views/player/players.types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE, TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { STATISTIC_TYPE_LABELS } from '@/modules/playerStatistic/utils/playerStatisticDisplay';
import {
  formatArgentinePhone,
  formatDocumentNumber,
  isAtLeastMinimumPlayerAge,
  isValidDocumentNumber,
  isValidPhone,
  VALIDATION_MESSAGES,
} from '@/modules/core/utils/validators';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';

const EMPTY_PLAYER_FORM: PlayerFormState = {
  firstName: '',
  secondName: '',
  lastName: '',
  documentNumber: '',
  birthDate: '',
  phoneNumber: '',
  socialSecurity: '',
  teamId: '',
  jerseyNumber: '',
};

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
  const { player, getPlayerById, putPlayerById } = usePlayer();
  const {
    playerStatistics,
    getPlayerStatisticsByFilter,
    playerCard,
    getPlayerCard,
    playerHistory,
    getPlayerHistory,
  } = usePlayerStatistic();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const { teams, getTeamsByFiltered } = useTeam();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'ficha' | 'historial' | 'puntuaciones' | 'sanciones'
  >('detalle');
  const [statisticDialogOpen, setStatisticDialogOpen] = useState(false);
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editSubmitting, setEditSubmitting] = useState(false);
  const [playerForm, setPlayerForm] = useState<PlayerFormState>(EMPTY_PLAYER_FORM);

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

  const phoneError =
    playerForm.phoneNumber.length > 0 && !isValidPhone(playerForm.phoneNumber);
  const documentNumberError =
    playerForm.documentNumber.length > 0 &&
    !isValidDocumentNumber(playerForm.documentNumber);
  const birthDateError =
    playerForm.birthDate.length > 0 &&
    !isAtLeastMinimumPlayerAge(playerForm.birthDate);

  const handlePlayerFieldChange = useCallback(
    (field: PlayerFormField, value: string) => {
      setPlayerForm(prev => ({
        ...prev,
        [field]: field === 'documentNumber' ? value.replace(/\D/g, '') : value,
      }));
    },
    []
  );

  const openEditDialog = () => {
    if (!player) return;

    setPlayerForm({
      firstName: player.firstName,
      secondName: player.secondName ?? '',
      lastName: player.lastName,
      documentNumber: player.documentNumber,
      birthDate: toDateInputValue(player.birthDate),
      phoneNumber: player.phoneNumber ?? '',
      socialSecurity: player.socialSecurity ?? '',
      teamId: player.teamId,
      jerseyNumber: '',
    });
    void getTeamsByFiltered({ pageSize: TABLE_ROWS_PER_PAGE });
    setEditDialogOpen(true);
  };

  const handleEditSubmit = async () => {
    if (!player) return;

    if (
      !playerForm.firstName.trim() ||
      !playerForm.lastName.trim() ||
      !playerForm.documentNumber.trim() ||
      !playerForm.birthDate.trim() ||
      !playerForm.phoneNumber.trim() ||
      !playerForm.socialSecurity.trim() ||
      !playerForm.teamId
    ) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre, apellido, documento, fecha de nacimiento, teléfono, seguro social y equipo son obligatorios.',
      });
      return;
    }

    if (!isValidPhone(playerForm.phoneNumber)) {
      void notifyWarning({ title: 'Teléfono inválido', text: `${VALIDATION_MESSAGES.phone}.` });
      return;
    }

    if (!isValidDocumentNumber(playerForm.documentNumber)) {
      void notifyWarning({
        title: 'Documento inválido',
        text: `${VALIDATION_MESSAGES.documentNumber}.`,
      });
      return;
    }

    if (!isAtLeastMinimumPlayerAge(playerForm.birthDate)) {
      void notifyWarning({
        title: 'Fecha de nacimiento inválida',
        text: `${VALIDATION_MESSAGES.minimumPlayerAge}.`,
      });
      return;
    }

    setEditSubmitting(true);
    const payload: IPutPlayerRequest = {
      firstName: playerForm.firstName.trim(),
      secondName: playerForm.secondName.trim() || undefined,
      lastName: playerForm.lastName.trim(),
      documentNumber: playerForm.documentNumber.trim(),
      birthDate: new Date(playerForm.birthDate),
      phoneNumber: playerForm.phoneNumber.trim(),
      socialSecurity: playerForm.socialSecurity.trim(),
      teamId: playerForm.teamId as GUID,
    };

    const updatedPlayer = await putPlayerById(player.id, payload);
    setEditSubmitting(false);

    if (!updatedPlayer) {
      return;
    }

    setEditDialogOpen(false);
    await getPlayerById(player.id, isAdministrative);
    await notifySuccess({
      title: 'Jugador actualizado',
      text: 'El jugador se actualizó correctamente.',
    });
  };

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
      actions={
        <Button variant="outlined" color="primary" onClick={openEditDialog}>
          Editar jugador
        </Button>
      }
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
              <Typography>{formatDocumentNumber(player.documentNumber)}</Typography>
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
              <Typography>
                {player.phoneNumber
                  ? formatArgentinePhone(player.phoneNumber)
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
      <PlayerFormDialog
        open={editDialogOpen}
        title="Editar jugador"
        confirmLabel="Guardar"
        form={playerForm}
        submitting={editSubmitting}
        confirmDisabled={phoneError || documentNumberError || birthDateError}
        showTeamSelect
        teamOptions={teams ?? []}
        onTeamChange={nextTeamId =>
          setPlayerForm(prev => ({ ...prev, teamId: nextTeamId }))
        }
        onFieldChange={handlePlayerFieldChange}
        onClose={() => setEditDialogOpen(false)}
        onConfirm={() => void handleEditSubmit()}
      />
    </PageShell>
  );
};

export default PlayerPage;
