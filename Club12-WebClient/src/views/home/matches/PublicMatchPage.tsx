import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  Divider,
  Stack,
  Typography,
} from '@mui/material';
import { useMatch } from '@/modules/match/hook/match.hook';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchStatusChip from '@/views/match/MatchStatusChip';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { formatMatchScore } from '@/modules/match/utils/matchDisplay';
import { formatLongDateTimeAr } from '@/modules/core/utils/formatDate';

const formatMatchDateTime = (value: string) => formatLongDateTimeAr(value);

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

  const goToTournaments = () => navigate(APP_ROUTES.publicTournaments);

  if (loading) {
    return (
      <PageShell maxWidth="md">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!match || (match.id !== matchId && match.slug !== matchId)) {
    return (
      <PageShell maxWidth="md">
        <Typography variant="h5" component="h1" sx={{ mb: 2 }}>
          Partido no encontrado
        </Typography>
        <Typography sx={{ color: 'text.secondary', mb: 3 }}>
          El partido que buscás no existe o ya no está disponible.
        </Typography>
        <Button onClick={goToTournaments}>
          Volver a torneos
        </Button>
      </PageShell>
    );
  }

  const { homeTeam, visitorTeam, isFinished, venue } = match;

  return (
    <PageShell
      maxWidth="md"
      back={{ label: 'Volver a torneos', onClick: goToTournaments }}
    >
      <Stack sx={{ alignItems: 'center', mb: 3 }} spacing={1}>
        <MatchStatusChip status={match.status} isFinished={isFinished} />
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
          {isFinished ? formatMatchScore(homeTeam?.score ?? 0, visitorTeam?.score ?? 0) : 'vs'}
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
    </PageShell>
  );
}
