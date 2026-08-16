import { Box, Card, CardContent, Chip, Divider, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import TeamLogo from '@/views/core/components/TeamLogo';

const formatDate = (value?: string | null) => {
  if (!value) return '—';
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleString('es-AR', { dateStyle: 'medium', timeStyle: 'short' });
};

export default function MatchCard({ match }: { match: IMatchResponse }) {
  const home = match.homeTeam;
  const visitor = match.visitorTeam;
  const finished = match.isFinished;

  return (
    <Card variant="outlined">
      <CardContent sx={{ pb: '12px !important' }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1.5}>
          <Typography variant="caption" color="text.secondary">
            {formatDate(match.matchDate)}
          </Typography>
          <Chip
            label={finished ? 'Finalizado' : 'Programado'}
            size="small"
            color={finished ? 'success' : 'default'}
            variant="outlined"
          />
        </Stack>

        <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
          <Stack alignItems="center" spacing={0.5} sx={{ flex: 1 }}>
            <TeamLogo teamName={home?.name ?? '?'} logoUrl={home?.logoUrl} size={40} />
            <Typography variant="body2" textAlign="center" fontWeight={500} lineHeight={1.2}>
              {home?.name ?? '—'}
            </Typography>
          </Stack>

          <Box textAlign="center" sx={{ minWidth: 64 }}>
            {finished ? (
              <Typography variant="h5" component="span" fontWeight="bold">
                {home?.score ?? 0} – {visitor?.score ?? 0}
              </Typography>
            ) : (
              <Typography variant="h6" component="span" color="text.secondary">
                vs
              </Typography>
            )}
          </Box>

          <Stack alignItems="center" spacing={0.5} sx={{ flex: 1 }}>
            <TeamLogo teamName={visitor?.name ?? '?'} logoUrl={visitor?.logoUrl} size={40} />
            <Typography variant="body2" textAlign="center" fontWeight={500} lineHeight={1.2}>
              {visitor?.name ?? '—'}
            </Typography>
          </Stack>
        </Stack>

        {match.venue && (
          <>
            <Divider sx={{ my: 1 }} />
            <Typography variant="caption" color="text.secondary">
              {match.venue.name}
            </Typography>
          </>
        )}
      </CardContent>
    </Card>
  );
}
