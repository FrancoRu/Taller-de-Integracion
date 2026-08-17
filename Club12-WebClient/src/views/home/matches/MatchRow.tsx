import { Box, Chip, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { IMatchResponse } from '@/modules/match/type/match.d';
import TeamLogo from '@/views/core/components/TeamLogo';
import { getMatchStatusColor, getMatchStatusLabel } from '@/modules/match/utils/matchDisplay';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

const formatTime = (value: string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime())
    ? '—'
    : parsed.toLocaleTimeString('es-AR', { hour: '2-digit', minute: '2-digit' });
};

interface TeamSideProps {
  name: string;
  logoUrl?: string;
  align: 'left' | 'right';
}

function TeamSide({ name, logoUrl, align }: TeamSideProps) {
  const logo = <TeamLogo teamName={name} logoUrl={logoUrl} size={28} />;
  const label = (
    <Typography
      variant="body2"
      noWrap
      sx={{
        fontWeight: 500,
        minWidth: 0
      }}>
      {name}
    </Typography>
  );

  return (
    <Stack
      direction={align === 'left' ? 'row' : 'row-reverse'}
      spacing={1}
      sx={{
        alignItems: "center",
        flex: 1,
        minWidth: 0,
        justifyContent: align === 'left' ? 'flex-start' : 'flex-end'
      }}>
      {logo}
      {label}
    </Stack>
  );
}

export default function MatchRow({ match }: { match: IMatchResponse }) {
  const home = match.homeTeam;
  const visitor = match.visitorTeam;
  const finished = match.isFinished;

  return (
    <Box
      component={Link}
      to={APP_ROUTES.publicMatch.build(match.id)}
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '40px 1fr auto 1fr auto', sm: '56px 1fr auto 1fr 140px' },
        alignItems: 'center',
        gap: { xs: 1, sm: 2 },
        px: 2,
        py: 1.25,
        textDecoration: 'none',
        color: 'inherit',
        borderRadius: 1,
        transition: 'background-color 0.15s ease',
        '&:hover': { bgcolor: 'action.hover' },
      }}
    >
      <Typography variant="caption" sx={{
        color: "text.secondary"
      }}>
        {formatTime(match.matchDate)}
      </Typography>

      <TeamSide name={home?.name ?? '—'} logoUrl={home?.logoUrl} align="right" />

      <Box
        sx={{
          textAlign: "center",
          minWidth: 56
        }}>
        {finished ? (
          <Typography variant="body1" sx={{
            fontWeight: "bold"
          }}>
            {home?.score ?? 0} – {visitor?.score ?? 0}
          </Typography>
        ) : (
          <Typography variant="body2" sx={{
            color: "text.secondary"
          }}>
            vs
          </Typography>
        )}
      </Box>

      <TeamSide name={visitor?.name ?? '—'} logoUrl={visitor?.logoUrl} align="left" />

      <Stack
        spacing={0.5}
        sx={{ alignItems: 'flex-end', justifyContent: 'center' }}
      >
        <Chip
          label={getMatchStatusLabel(finished)}
          size="small"
          color={getMatchStatusColor(finished)}
          variant="outlined"
        />
        {match.venue && (
          <Typography
            variant="caption"
            noWrap
            sx={{ color: 'text.secondary', display: { xs: 'none', sm: 'block' } }}
          >
            {match.venue.name}
          </Typography>
        )}
      </Stack>
    </Box>
  );
}
