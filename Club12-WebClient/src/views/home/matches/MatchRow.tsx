import { Box, Chip, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import { IMatchResponse } from '@/modules/match/type/match.d';
import TeamLogo from '@/views/core/components/TeamLogo';
import { formatMatchScore, getMatchStatusColor, getMatchStatusLabel } from '@/modules/match/utils/matchDisplay';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { formatDateAr, formatTimeAr } from '@/modules/core/utils/formatDate';
import { AccessTimeIcon, StadiumIcon } from '@/views/core/MUI/icons/icons';

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
      direction="row"
      spacing={1}
      sx={{
        alignItems: "center",
        flex: 1,
        minWidth: 0,
        justifyContent: align === 'left' ? 'flex-start' : 'flex-end'
      }}>
      {/* The crest always sits closest to the score in the middle — for the
          right-aligned (home) side that means the name reads first and the
          crest second, so both sides flank the score symmetrically instead
          of the crest always leading in DOM order regardless of side. */}
      {align === 'right' ? (
        <>
          {label}
          {logo}
        </>
      ) : (
        <>
          {logo}
          {label}
        </>
      )}
    </Stack>
  );
}

const defaultBuildHref = (match: IMatchResponse) =>
  APP_ROUTES.publicMatch.build(match.slug ?? match.id);

export default function MatchRow({
  match,
  buildHref = defaultBuildHref,
}: {
  match: IMatchResponse;
  /**
   * Builds the row's link target. Defaults to the public match page; admin
   * callers pass a builder pointing at the panel match page instead.
   */
  buildHref?: (match: IMatchResponse) => string;
}) {
  const home = match.homeTeam;
  const visitor = match.visitorTeam;
  const finished = match.isFinished;

  return (
    <Box
      component={Link}
      to={buildHref(match)}
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '56px 1fr auto 1fr', sm: '56px 1fr auto 1fr 140px' },
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
      <Stack spacing={0} sx={{ minWidth: 0 }}>
        <Typography variant="caption" sx={{ color: 'text.secondary', whiteSpace: 'nowrap' }}>
          {formatDateAr(match.matchDate)}
        </Typography>
        <Stack direction="row" spacing={0.4} sx={{ alignItems: 'center' }}>
          <AccessTimeIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
          <Typography variant="caption" sx={{ color: 'text.secondary', whiteSpace: 'nowrap', fontWeight: 500 }}>
            {formatTimeAr(match.matchDate)}
          </Typography>
        </Stack>
      </Stack>

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
            {formatMatchScore(home?.score ?? 0, visitor?.score ?? 0)}
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
        sx={{
          display: { xs: 'none', sm: 'flex' },
          alignItems: 'flex-end',
          justifyContent: 'center',
        }}
      >
        <Chip
          label={getMatchStatusLabel(finished)}
          size="small"
          color={getMatchStatusColor(finished)}
          variant="outlined"
        />
        {match.venue && (
          <Stack
            direction="row"
            spacing={0.4}
            sx={{ alignItems: 'center', display: { xs: 'none', sm: 'flex' } }}
          >
            <StadiumIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
            <Typography variant="caption" noWrap sx={{ color: 'text.secondary' }}>
              {match.venue.name}
            </Typography>
          </Stack>
        )}
      </Stack>
    </Box>
  );
}
