import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  Button,
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
import { IPutTeamRequest } from '@/modules/team/type/team.d';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import JerseySvg from '@/views/core/components/JerseySvg';
import PlayersPage, {
  PlayerMedicalInfo,
} from '@/views/player/PlayersPage';
import NewEntityButton from '@/views/core/components/NewEntityButton';
import PlayerStatisticCreatePage from '@/views/playerStatistic/playerStatisticCreatePage';
import PlayerSanctionCreatePage from '@/views/playerSanction/playerSanctionCreatePage';
import RosterCsvImportDialog from '@/views/team/RosterCsvImportDialog';
import TeamFormDialog from '@/views/team/TeamFormDialog';
import TeamStaffManager from '@/views/team/TeamStaffManager';
import type { TeamFormState, TeamFormField } from '@/views/team/teams.types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { STATISTIC_TYPE_LABELS } from '@/modules/playerStatistic/utils/playerStatisticDisplay';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';

const EMPTY_TEAM_FORM: TeamFormState = {
  name: '',
  threeLetterCode: '',
  shirtColor: '#1E5FCC',
  shirtSecondaryColor: '',
  shirtTertiaryColor: '',
  jerseyStyle: 'solid',
  logo: null,
  logoUrl: '',
};

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
  const { team, getTeamById, putTeamById, putTeamLogoById } = useTeam();
  const { playerStatistics, getPlayerStatisticsByFilter } = usePlayerStatistic();
  const { playerSanctions, getPlayerSanctionByFilter } = usePlayerSanction();
  const [loading, setLoading] = useState(false);
  type TeamTab =
    | 'detalle'
    | 'jugadores'
    | 'puntuaciones'
    | 'sanciones'
    | 'cuerpoTecnico';
  const TAB_QUERY_PARAM = 'tab';
  // Kept in the URL (not local state) so leaving to e.g. a player's detail
  // and clicking "Volver" back here lands on the same tab instead of
  // resetting to Detalle.
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get(TAB_QUERY_PARAM) ?? 'detalle') as TeamTab;
  const setTab = (value: TeamTab) => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.set(TAB_QUERY_PARAM, value);
        return next;
      },
      { replace: true }
    );
  };
  const [statisticDialogOpen, setStatisticDialogOpen] = useState(false);
  const [sanctionDialogOpen, setSanctionDialogOpen] = useState(false);
  const [rosterImportOpen, setRosterImportOpen] = useState(false);
  // Bumped after a CSV import so PlayersPage re-fetches the admin roster
  // list: RosterCsvImportDialog creates players one by one and each POST
  // response is a PublicPlayerResponse (no documentNumber/birthDate/
  // phoneNumber/socialSecurity — those are admin-only), so the shared
  // player-context state gets upserted with that partial shape. Without a
  // real re-fetch of the full AdminPlayerResponse list, the grid keeps
  // showing those fields blank even though they're correctly persisted.
  const [rosterRefreshTrigger, setRosterRefreshTrigger] = useState(0);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [teamForm, setTeamForm] = useState<TeamFormState>(EMPTY_TEAM_FORM);
  const [editSubmitting, setEditSubmitting] = useState(false);

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

  // These filter by a real GUID teamId — unlike the team fetch itself,
  // they can never accept the route's idOrSlug value, so they must wait
  // for `team` to resolve rather than using targetTeamId.
  const refreshStatistics = () => {
    if (!team?.id) return;
    void getPlayerStatisticsByFilter({ teamId: team.id, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  const refreshSanctions = () => {
    if (!team?.id) return;
    void getPlayerSanctionByFilter({ teamId: team.id, pageSize: FILTER_OPTIONS_PAGE_SIZE });
  };

  useEffect(() => {
    if (tab === 'puntuaciones') {
      refreshStatistics();
    } else if (tab === 'sanciones') {
      refreshSanctions();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, team?.id]);

  const playerNameById = useMemo(() => {
    const map = new Map<GUID, string>();
    (team?.players ?? []).forEach(player => map.set(player.id, player.fullName));
    return map;
  }, [team?.players]);

  // Per-player habilitación / medical status for this season roster, so the
  // plantel can show the badge and drive the ficha-médica dialog (HU-57/HU-62).
  const medicalByPlayerId = useMemo(() => {
    const map = new Map<GUID, PlayerMedicalInfo>();
    (team?.players ?? []).forEach(player =>
      map.set(player.id, {
        status: player.medicalRecordStatus,
        isHabilitado: player.isHabilitado,
      })
    );
    return map;
  }, [team?.players]);

  // Per-player dorsal for this season roster, so the plantel can show the
  // number and prefill the assign-dorsal dialog (HU-54).
  const jerseyByPlayerId = useMemo(() => {
    const map = new Map<GUID, number | null | undefined>();
    (team?.players ?? []).forEach(player =>
      map.set(player.id, player.jerseyNumber)
    );
    return map;
  }, [team?.players]);

  const refreshTeam = useCallback(() => {
    if (!targetTeamId) return;
    void getTeamById(targetTeamId);
  }, [getTeamById, targetTeamId]);

  const handleTeamFieldChange = useCallback((field: TeamFormField, value: string) => {
    setTeamForm(prev => ({
      ...prev,
      [field]: field === 'threeLetterCode' ? value.toUpperCase() : value,
    }));
  }, []);

  const handleLogoChange = useCallback((file: File | null) => {
    setTeamForm(prev => ({ ...prev, logo: file }));
  }, []);

  const openEditDialog = () => {
    if (!team) return;

    setTeamForm({
      name: team.name,
      threeLetterCode: team.threeLetterCode,
      shirtColor: team.shirtColor,
      shirtSecondaryColor: team.shirtSecondaryColor ?? '',
      shirtTertiaryColor: team.shirtTertiaryColor ?? '',
      jerseyStyle: team.jerseyStyle ?? 'solid',
      logo: null,
      logoUrl: team.logoUrl ?? '',
    });
    setEditDialogOpen(true);
  };

  const handleEditSubmit = async () => {
    if (!team) return;

    if (!teamForm.name.trim() || !teamForm.threeLetterCode.trim()) {
      void notifyWarning({
        title: 'Campos incompletos',
        text: 'Nombre y código son obligatorios.',
      });
      return;
    }

    setEditSubmitting(true);
    const payload: IPutTeamRequest = {
      name: teamForm.name.trim(),
      threeLetterCode: teamForm.threeLetterCode.trim(),
      shirtColor: teamForm.shirtColor.trim(),
      shirtSecondaryColor: teamForm.shirtSecondaryColor.trim() || null,
      shirtTertiaryColor: teamForm.shirtTertiaryColor.trim() || null,
      jerseyStyle: teamForm.jerseyStyle,
    };

    const ok = await putTeamById(team.id, payload);

    if (!ok) {
      setEditSubmitting(false);
      return;
    }

    // The team fields and its logo are two separate endpoints; upload the new
    // logo (if the admin picked one) as part of the same save.
    if (teamForm.logo) {
      await putTeamLogoById(team.id, teamForm.logo);
    }
    setEditSubmitting(false);

    setEditDialogOpen(false);
    refreshTeam();
    await notifySuccess({
      title: 'Equipo actualizado',
      text: 'El equipo se actualizó correctamente.',
    });
  };

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
              onClick={() => navigate(-1)}
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
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        sx={{
          alignItems: { xs: 'flex-start', sm: 'center' },
          justifyContent: "space-between",
          mb: 2
        }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: "center" }}>
          <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={44} />
          <Typography variant="h6">{team.name}</Typography>
        </Stack>
        <Stack direction="row" spacing={1.5}>
          <Button variant="outlined" color="primary" onClick={openEditDialog}>
            Editar equipo
          </Button>
          {!hideBackLink && (
            <Button
              variant="contained"
              color="primary"
              onClick={() => navigate(-1)}
            >
              Volver
            </Button>
          )}
        </Stack>
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
        {team.tournamentId && (
          <Tab label="Cuerpo técnico" value="cuerpoTecnico" />
        )}
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
              Camiseta
            </Typography>
            <JerseySvg
              color={team.shirtColor}
              secondaryColor={team.shirtSecondaryColor}
              tertiaryColor={team.shirtTertiaryColor}
              style={team.jerseyStyle}
              size={48}
              title={`Camiseta de ${team.name}`}
            />
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
          {team.clubId && (
            <Grid size={12}>
              <Button
                variant="outlined"
                color="primary"
                onClick={() =>
                  navigate(APP_ROUTES.panelClub.build(team.clubId as GUID))
                }
              >
                Ver historial del club
              </Button>
            </Grid>
          )}
        </Grid>
      )}

      {tab === 'jugadores' && team.id && (
        <>
          <Stack
            direction="row"
            sx={{
              justifyContent: 'flex-end',
              mb: 2,
            }}
          >
            <Button
              variant="contained"
              color="primary"
              onClick={() => setRosterImportOpen(true)}
            >
              Importar plantel desde CSV
            </Button>
          </Stack>
          <PlayersPage
            teamId={team.id}
            title={undefined}
            emptyMessage="Este equipo no tiene jugadores cargados."
            wrapInCard={false}
            tournamentId={team.tournamentId}
            medicalByPlayerId={medicalByPlayerId}
            jerseyByPlayerId={jerseyByPlayerId}
            onMedicalChange={refreshTeam}
            refreshTrigger={rosterRefreshTrigger}
          />
        </>
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

      {tab === 'cuerpoTecnico' && team.tournamentId && (
        <TeamStaffManager teamId={team.id} tournamentId={team.tournamentId} />
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
      {team.id && (
        <RosterCsvImportDialog
          open={rosterImportOpen}
          onClose={() => setRosterImportOpen(false)}
          teamId={team.id}
          onImported={() => {
            refreshTeam();
            setRosterRefreshTrigger(trigger => trigger + 1);
          }}
        />
      )}
      <TeamFormDialog
        withLogo
        open={editDialogOpen}
        title="Editar equipo"
        confirmLabel="Guardar"
        form={teamForm}
        submitting={editSubmitting}
        onFieldChange={handleTeamFieldChange}
        onLogoChange={handleLogoChange}
        onClose={() => setEditDialogOpen(false)}
        onConfirm={() => void handleEditSubmit()}
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
