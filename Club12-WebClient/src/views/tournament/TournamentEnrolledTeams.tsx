import React, { useCallback, useEffect, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Button,
  Link,
  List,
  ListItem,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { IEnrollTeamRequest } from '@/modules/tournament/type/tournament';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { FILTER_OPTIONS_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { notifySuccess } from '@/modules/core/utils/confirmDialog';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import EnrollTeamDialog from '@/views/tournament/EnrollTeamDialog';

interface TournamentEnrolledTeamsProps {
  tournamentId: GUID;
}

/**
 * Registration-phase team management for a tournament (HU-107). Lists the teams
 * enrolled in the tournament (each linking to its roster tab for editing — see
 * HU-51), and lets the admin enroll a new or existing team. Rendered only while
 * the tournament is OpenForRegistration (gated by the parent detail view).
 */
const TournamentEnrolledTeams: React.FC<TournamentEnrolledTeamsProps> = ({
  tournamentId,
}) => {
  const { getTeamsByFiltered } = useTeam();
  const { enrollTeam } = useTournament();

  const [enrolledTeams, setEnrolledTeams] = useState<ITeamResponse[]>([]);
  const [allTeams, setAllTeams] = useState<ITeamResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const loadEnrolledTeams = useCallback(async () => {
    const response = await getTeamsByFiltered({
      tournamentId,
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
    setEnrolledTeams(response?.items ?? []);
  }, [getTeamsByFiltered, tournamentId]);

  const loadAllTeams = useCallback(async () => {
    const response = await getTeamsByFiltered({
      pageSize: FILTER_OPTIONS_PAGE_SIZE,
    });
    setAllTeams(response?.items ?? []);
  }, [getTeamsByFiltered]);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        await loadEnrolledTeams();
        await loadAllTeams();
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [loadEnrolledTeams, loadAllTeams]);

  const enrolledIds = new Set(enrolledTeams.map(team => team.id));
  const availableTeams = allTeams.filter(team => !enrolledIds.has(team.id));

  const handleEnroll = async (request: IEnrollTeamRequest) => {
    setSubmitting(true);
    try {
      const success = await enrollTeam(tournamentId, request);
      if (!success) {
        return;
      }

      setDialogOpen(false);
      await loadEnrolledTeams();
      await loadAllTeams();
      await notifySuccess({
        title: 'Equipo inscripto',
        text: 'El equipo se inscribió correctamente en el torneo.',
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box sx={{ width: '100%' }}>
      <Stack
        direction="row"
        sx={{
          justifyContent: 'space-between',
          alignItems: 'center',
          mb: 2,
        }}
      >
        <Typography variant="h6">Equipos inscriptos</Typography>
        <Button variant="contained" onClick={() => setDialogOpen(true)}>
          Inscribir equipo
        </Button>
      </Stack>

      {loading ? (
        <LoadingIndicator />
      ) : enrolledTeams.length === 0 ? (
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          Todavía no hay equipos inscriptos en este torneo.
        </Typography>
      ) : (
        <List>
          {enrolledTeams.map(team => (
            <ListItem key={team.id} divider>
              <ListItemText
                primary={
                  <Link
                    component={RouterLink}
                    to={APP_ROUTES.panelTeamDetail.build(team.slug)}
                    underline="hover"
                  >
                    {team.name}
                  </Link>
                }
                secondary="Editá el plantel desde la ficha del equipo"
              />
            </ListItem>
          ))}
        </List>
      )}

      <EnrollTeamDialog
        open={dialogOpen}
        submitting={submitting}
        availableTeams={availableTeams}
        onClose={() => setDialogOpen(false)}
        onConfirm={request => void handleEnroll(request)}
      />
    </Box>
  );
};

export default TournamentEnrolledTeams;
