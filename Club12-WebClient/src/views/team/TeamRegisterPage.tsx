import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  CircularProgress,
  FormControl,
  InputLabel,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material';
import { notifySuccess, notifyWarning } from '@/modules/core/utils/confirmDialog';
import { GUID } from '@/modules/core/types/types';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team.d';
import TeamLogo from '@/views/core/components/TeamLogo';

const TeamRegisterPage: React.FC = () => {
  const {
    tournaments,
    getAllTournamentsByFilter,
    registerTeamsByTournamentId,
  } = useTournament();
  const { teams, getTeamsByFiltered } = useTeam();

  const [selectedTournamentId, setSelectedTournamentId] = useState<GUID | ''>(
    ''
  );
  const [checkedTeamIds, setCheckedTeamIds] = useState<Set<GUID>>(new Set());
  const [loadingTournaments, setLoadingTournaments] = useState(false);
  const [loadingTeams, setLoadingTeams] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  useEffect(() => {
    const fetch = async () => {
      setLoadingTournaments(true);
      // Fetch every tournament, not just the OpenForRegistration ones — a
      // team can be currently assigned to a tournament in ANY status, and
      // its name is needed to explain why that team isn't selectable here
      // (see visibleTeams below). tournamentOptions still filters this same
      // list down to OpenForRegistration for the "Torneo" select.
      await getAllTournamentsByFilter({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
      setLoadingTournaments(false);
    };
    void fetch();
  }, [getAllTournamentsByFilter]);

  /** When tournament selection changes, load all teams and filter client-side. */
  useEffect(() => {
    if (!selectedTournamentId) {
      return;
    }

    const fetch = async () => {
      // Load the full team catalogue (not just one table page) so every
      // enrollable team — including clubs not assigned to any tournament — is
      // available to filter client-side.
      setLoadingTeams(true);
      await getTeamsByFiltered({ pageSize: FILTER_OPTIONS_PAGE_SIZE });
      setLoadingTeams(false);
    };

    void fetch();
  }, [selectedTournamentId, getTeamsByFiltered]);

  /** Tournament name lookup, so a team tied up elsewhere can say where. */
  const tournamentNameById = useMemo(
    () => new Map((tournaments ?? []).map(t => [t.id, t.name])),
    [tournaments]
  );

  /** Every team. One tied up in a DIFFERENT tournament is still shown here
   * — disabled, labelled with that tournament's name — instead of silently
   * disappearing, so it's clear WHY it isn't selectable (it must be
   * unenrolled from its current tournament first, from that tournament's
   * "Equipos inscriptos" tab) rather than looking like there are simply no
   * teams left to register.
   */
  const visibleTeams = useMemo<ITeamResponse[]>(() => {
    if (!selectedTournamentId || !teams) {
      return [];
    }
    return teams;
  }, [teams, selectedTournamentId]);

  /** Sync checkboxes when visible teams change: pre-check those already in the tournament. */
  useEffect(() => {
    if (!selectedTournamentId) {
      return;
    }
    const preChecked = new Set<GUID>(
      visibleTeams
        .filter(t => t.tournamentId === selectedTournamentId)
        .map(t => t.id)
    );
    setCheckedTeamIds(preChecked);
  }, [visibleTeams, selectedTournamentId]);

  const handleToggle = useCallback((teamId: GUID) => {
    setCheckedTeamIds(prev => {
      const next = new Set(prev);
      if (next.has(teamId)) {
        next.delete(teamId);
      } else {
        next.add(teamId);
      }
      return next;
    });
  }, []);

  const handleRegister = async () => {
    if (!selectedTournamentId) {
      return;
    }

    if (checkedTeamIds.size === 0) {
      void notifyWarning({
        title: 'Sin equipos seleccionados',
        text: 'Seleccioná al menos un equipo para registrar.',
      });
      return;
    }

    setSubmitting(true);
    const success = await registerTeamsByTournamentId(
      selectedTournamentId as GUID,
      Array.from(checkedTeamIds)
    );
    setSubmitting(false);

    if (success) {
      await notifySuccess({
        title: 'Registro exitoso',
        text: 'Los equipos fueron registrados correctamente.',
      });
    }
  };

  const tournamentOptions = useMemo(
    () =>
      (tournaments ?? []).filter(
        t => t.status === TournamentStatus.OpenForRegistration
      ),
    [tournaments]
  );

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 3
        }}>
          Registro de Equipo
        </Typography>

        <Stack spacing={3}>
          {loadingTournaments ? (
            <Box
              sx={{
                display: "flex",
                justifyContent: "center",
                py: 1
              }}>
              <CircularProgress size={20} />
            </Box>
          ) : tournamentOptions.length === 0 ? (
            <Typography variant="body2" sx={{
              color: "text.secondary"
            }}>
              No hay torneos con la inscripción abierta
            </Typography>
          ) : (
            <FormControl fullWidth size="small">
              <InputLabel id="tournament-select-label">Torneo</InputLabel>
              <Select
                labelId="tournament-select-label"
                label="Torneo"
                value={selectedTournamentId}
                onChange={e =>
                  setSelectedTournamentId(e.target.value as GUID | '')
                }
              >
                <MenuItem value="" disabled>
                  Seleccionar torneo
                </MenuItem>
                {tournamentOptions.map(t => (
                  <MenuItem key={t.id} value={t.id}>
                    {t.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}

          {selectedTournamentId && (
            <>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Equipos disponibles para registrar
              </Typography>

              {loadingTeams ? (
                <Box
                  sx={{
                    display: "flex",
                    justifyContent: "center",
                    py: 3
                  }}>
                  <CircularProgress />
                </Box>
              ) : visibleTeams.length === 0 ? (
                <Typography variant="body2" sx={{
                  color: "text.secondary"
                }}>
                  No hay equipos disponibles para este torneo.
                </Typography>
              ) : (
                <List dense disablePadding>
                  {visibleTeams.map(team => {
                    const registeredElsewhere =
                      Boolean(team.tournamentId) &&
                      team.tournamentId !== selectedTournamentId;
                    const otherTournamentName = registeredElsewhere
                      ? tournamentNameById.get(team.tournamentId as GUID)
                      : undefined;

                    return (
                      <ListItem
                        key={team.id}
                        disablePadding
                        onClick={
                          registeredElsewhere
                            ? undefined
                            : () => handleToggle(team.id)
                        }
                        sx={{
                          cursor: registeredElsewhere ? 'default' : 'pointer',
                          borderRadius: 1,
                          px: 1,
                          opacity: registeredElsewhere ? 0.6 : 1,
                          '&:hover': registeredElsewhere
                            ? undefined
                            : { backgroundColor: 'action.hover' },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 36 }}>
                          <Checkbox
                            edge="start"
                            checked={checkedTeamIds.has(team.id)}
                            disabled={registeredElsewhere}
                            tabIndex={-1}
                            disableRipple
                            color="primary"
                          />
                        </ListItemIcon>
                        <ListItemIcon sx={{ minWidth: 36 }}>
                          <TeamLogo
                            teamName={team.name}
                            logoUrl={team.logoUrl}
                            size={28}
                          />
                        </ListItemIcon>
                        <ListItemText
                          primary={team.name}
                          secondary={
                            registeredElsewhere
                              ? `Registrado en ${otherTournamentName ?? 'otro torneo'} — desinscribilo desde ahí primero`
                              : team.tournamentId
                                ? 'Ya registrado'
                                : 'Sin torneo'
                          }
                        />
                      </ListItem>
                    );
                  })}
                </List>
              )}

              <Box
                sx={{
                  display: "flex",
                  justifyContent: "flex-end"
                }}>
                <Button
                  variant="contained"
                  disabled={
                    submitting || loadingTeams || checkedTeamIds.size === 0
                  }
                  onClick={() => void handleRegister()}
                >
                  {submitting ? (
                    <CircularProgress size={20} color="inherit" />
                  ) : (
                    'Registrar'
                  )}
                </Button>
              </Box>
            </>
          )}
        </Stack>
      </CardContent>
    </Card>
  );
};

export default TeamRegisterPage;
