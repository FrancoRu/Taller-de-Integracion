import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Grid,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useMatch } from '@/modules/match/hook/match.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchCard from '@/views/home/matches/MatchCard';

const STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programado',
  OpenForRegistration: 'Inscripción abierta',
  Ongoing: 'En curso',
  Finished: 'Finalizado',
  Canceled: 'Cancelado',
};

const formatDate = (value: Date | string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString('es-AR');
};

type Tab = 'info' | 'equipos' | 'partidos';

export default function PublicTournamentPage() {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { tournament, getTournamentById } = useTournament();
  const { teams, getTeamsByFiltered } = useTeam();
  const { matches, getMatchByFilter } = useMatch();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<Tab>('info');

  const getTournamentRef = useRef(getTournamentById);
  const getTeamsRef = useRef(getTeamsByFiltered);
  const getMatchesRef = useRef(getMatchByFilter);

  useEffect(() => { getTournamentRef.current = getTournamentById; }, [getTournamentById]);
  useEffect(() => { getTeamsRef.current = getTeamsByFiltered; }, [getTeamsByFiltered]);
  useEffect(() => { getMatchesRef.current = getMatchByFilter; }, [getMatchByFilter]);

  useEffect(() => {
    if (!tournamentId) return;
    const fetch = async () => {
      setLoading(true);
      await getTournamentRef.current(tournamentId);
      setLoading(false);
    };
    void fetch();
  }, [tournamentId]);

  useEffect(() => {
    if (!tournamentId || tab !== 'equipos') return;
    void getTeamsRef.current({ tournamentId, pageSize: 100, pageNumber: 1 });
  }, [tab, tournamentId]);

  useEffect(() => {
    if (!tournamentId || tab !== 'partidos') return;
    void getMatchesRef.current({ tournamentId, pageSize: 50, pageNumber: 1 });
  }, [tab, tournamentId]);

  const teamRows = useMemo(() => teams ?? [], [teams]);
  const matchRows = useMemo(() => matches ?? [], [matches]);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" py={10}>
        <CircularProgress />
      </Box>
    );
  }

  if (!tournament || tournament.id !== tournamentId) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" mb={2}>Torneo no encontrado</Typography>
        <Button onClick={() => navigate('/torneos')}>Volver a torneos</Button>
      </Container>
    );
  }

  const status = tournament.status as TournamentStatus;

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Button onClick={() => navigate('/torneos')} sx={{ mb: 3, pl: 0 }} color="inherit">
        ← Volver a torneos
      </Button>

      <Typography variant="h4" fontWeight="bold" mb={0.5}>
        {tournament.name}
      </Typography>
      <Typography variant="subtitle1" color="text.secondary" mb={3}>
        {STATUS_LABEL[status] ?? status}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Tabs
        value={tab}
        onChange={(_, value: Tab) => setTab(value)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Información" value="info" />
        <Tab label="Equipos" value="equipos" />
        <Tab label="Partidos" value="partidos" />
      </Tabs>

      {tab === 'info' && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Typography variant="subtitle2" color="text.secondary">Descripción</Typography>
            <Typography>{tournament.description || '—'}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" color="text.secondary">Fecha de inicio</Typography>
            <Typography>{formatDate(tournament.startDate)}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" color="text.secondary">Cierre de inscripción</Typography>
            <Typography>{formatDate(tournament.teamRegistrationDeadline)}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" color="text.secondary">Equipos mínimos</Typography>
            <Typography>{tournament.minTeams}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" color="text.secondary">Equipos máximos</Typography>
            <Typography>{tournament.maxTeams}</Typography>
          </Grid>
        </Grid>
      )}

      {tab === 'equipos' && (
        teamRows.length === 0 ? (
          <Typography color="text.secondary">No hay equipos inscriptos en este torneo.</Typography>
        ) : (
          <Grid container spacing={2}>
            {teamRows.map(team => (
              <Grid item xs={12} sm={6} md={4} key={team.id}>
                <Box
                  display="flex"
                  alignItems="center"
                  gap={1.5}
                  sx={{
                    p: 1.5,
                    border: '1px solid',
                    borderColor: 'divider',
                    borderRadius: 1,
                    cursor: 'pointer',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                  onClick={() => navigate(`/equipos/${team.id}`)}
                >
                  <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={36} />
                  <Box>
                    <Typography variant="body2" fontWeight={500}>{team.name}</Typography>
                    <Typography variant="caption" color="text.secondary">{team.threeLetterCode}</Typography>
                  </Box>
                </Box>
              </Grid>
            ))}
          </Grid>
        )
      )}

      {tab === 'partidos' && (
        matchRows.length === 0 ? (
          <Typography color="text.secondary">No hay partidos registrados en este torneo.</Typography>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gap: 2,
              gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', md: 'repeat(3, 1fr)' },
            }}
          >
            {matchRows.map(match => (
              <MatchCard key={match.id} match={match} />
            ))}
          </Box>
        )
      )}
    </Container>
  );
}
