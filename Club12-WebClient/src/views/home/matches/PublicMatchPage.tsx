import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMatch } from '@/modules/match/hook/match.hook';
import TeamLogo from '@/views/core/components/TeamLogo';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { getMatchStatusColor, getMatchStatusLabel } from '@/modules/match/utils/matchDisplay';

const formatMatchDateTime = (value: string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleString('es-AR', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        hour: '2-digit',
        minute: '2-digit',
      });
};

export default function PublicMatchPage() {
  const { matchId } = useParams<{ matchId: string }>();
  const navigate = useNavigate();
  const { match, getMatchById } = useMatch();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!matchId) return;
    const fetch = async () => {
      setLoading(true);
      await getMatchById(matchId);
      setLoading(false);
    };
    void fetch();
  }, [matchId, getMatchById]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!match || (match.id !== matchId && match.slug !== matchId)) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" sx={{ mb: 2 }}>
          Partido no encontrado
        </Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicMatches)}>
          Volver a partidos
        </Button>
      </Container>
    );
  }

  const { homeTeam, visitorTeam, isFinished, venue } = match;

  return (
    <Container maxWidth="md" sx={{ py: 5 }}>
      <Button
        onClick={() => navigate(APP_ROUTES.publicMatches)}
        sx={{ mb: 3, pl: 0 }}
        color="inherit"
      >
        ← Volver a partidos
      </Button>

      <Stack sx={{ alignItems: 'center', mb: 3 }} spacing={1}>
        <Chip
          label={getMatchStatusLabel(isFinished)}
          color={getMatchStatusColor(isFinished)}
          size="small"
        />
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          {formatMatchDateTime(match.matchDate)}
        </Typography>
      </Stack>

      <Stack
        direction="row"
        spacing={3}
        sx={{ alignItems: 'center', justifyContent: 'center', mb: 4 }}
      >
        <Stack sx={{ alignItems: 'center', flex: 1 }} spacing={1}>
          <TeamLogo teamName={homeTeam?.name ?? '—'} logoUrl={homeTeam?.logoUrl} size={64} />
          <Typography variant="h6" sx={{ textAlign: 'center' }}>
            {homeTeam?.name ?? '—'}
          </Typography>
        </Stack>

        <Typography variant="h3" component="p" sx={{ fontWeight: 700, px: 2 }}>
          {isFinished ? `${homeTeam?.score ?? 0} – ${visitorTeam?.score ?? 0}` : 'vs'}
        </Typography>

        <Stack sx={{ alignItems: 'center', flex: 1 }} spacing={1}>
          <TeamLogo teamName={visitorTeam?.name ?? '—'} logoUrl={visitorTeam?.logoUrl} size={64} />
          <Typography variant="h6" sx={{ textAlign: 'center' }}>
            {visitorTeam?.name ?? '—'}
          </Typography>
        </Stack>
      </Stack>

      <Divider sx={{ mb: 3 }} />

      <Stack spacing={2}>
        <Box>
          <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
            Cancha
          </Typography>
          <Typography>{venue?.name ?? 'A confirmar'}</Typography>
        </Box>
        {isFinished && match.winningTeamName && (
          <Box>
            <Typography variant="subtitle2" sx={{ color: 'text.secondary' }}>
              Ganador
            </Typography>
            <Typography>{match.winningTeamName}</Typography>
          </Box>
        )}
      </Stack>
    </Container>
  );
}
