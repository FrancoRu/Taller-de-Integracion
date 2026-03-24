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
import Swal from 'sweetalert2';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team.d';
import TeamLogo from '../core/components/TeamLogo';

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
      await getAllTournamentsByFilter({ pageSize: 300 });
      setLoadingTournaments(false);
    };
    void fetch();
  }, [getAllTournamentsByFilter]);

  // When tournament selection changes, load all teams and filter client-side
  useEffect(() => {
    if (!selectedTournamentId) {
      return;
    }

    const fetch = async () => {
      setLoadingTeams(true);
      await getTeamsByFiltered({ pageSize: 300 });
      setLoadingTeams(false);
    };

    void fetch();
  }, [selectedTournamentId, getTeamsByFiltered]);

  // Visible teams: those whose tournamentId is the selected one OR null/undefined
  const visibleTeams = useMemo<ITeamResponse[]>(() => {
    if (!selectedTournamentId || !teams) {
      return [];
    }
    return teams.filter(
      t => t.tournamentId === selectedTournamentId || t.tournamentId === null
    );
  }, [teams, selectedTournamentId]);

  // Sync checkboxes when visible teams change: pre-check those already in the tournament
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
      void Swal.fire({
        title: 'Sin equipos seleccionados',
        text: 'Seleccioná al menos un equipo para registrar.',
        icon: 'warning',
        confirmButtonColor: '#FD6B00',
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
      await Swal.fire({
        title: 'Registro exitoso',
        text: 'Los equipos fueron registrados correctamente.',
        icon: 'success',
        confirmButtonColor: '#FD6B00',
      });
    }
  };

  const tournamentOptions = tournaments ?? [];

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" mb={3}>
          Registro de Equipo
        </Typography>

        <Stack spacing={3}>
          <FormControl fullWidth size="small">
            <InputLabel id="tournament-select-label">Torneo</InputLabel>
            <Select
              labelId="tournament-select-label"
              label="Torneo"
              value={selectedTournamentId}
              onChange={e =>
                setSelectedTournamentId(e.target.value as GUID | '')
              }
              disabled={loadingTournaments}
              startAdornment={
                loadingTournaments ? (
                  <CircularProgress size={16} sx={{ mr: 1 }} />
                ) : undefined
              }
            >
              {tournamentOptions.map(t => (
                <MenuItem key={t.id} value={t.id}>
                  {t.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {selectedTournamentId && (
            <>
              <Typography variant="subtitle2" color="text.secondary">
                Equipos disponibles para registrar
              </Typography>

              {loadingTeams ? (
                <Box display="flex" justifyContent="center" py={3}>
                  <CircularProgress />
                </Box>
              ) : visibleTeams.length === 0 ? (
                <Typography variant="body2" color="text.secondary">
                  No hay equipos disponibles para este torneo.
                </Typography>
              ) : (
                <List dense disablePadding>
                  {visibleTeams.map(team => (
                    <ListItem
                      key={team.id}
                      disablePadding
                      onClick={() => handleToggle(team.id)}
                      sx={{
                        cursor: 'pointer',
                        borderRadius: 1,
                        px: 1,
                        '&:hover': { backgroundColor: 'action.hover' },
                      }}
                    >
                      <ListItemIcon sx={{ minWidth: 36 }}>
                        <Checkbox
                          edge="start"
                          checked={checkedTeamIds.has(team.id)}
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
                          team.tournamentId ? 'Ya registrado' : 'Sin torneo'
                        }
                      />
                    </ListItem>
                  ))}
                </List>
              )}

              <Box display="flex" justifyContent="flex-end">
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
