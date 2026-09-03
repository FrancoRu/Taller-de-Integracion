import { useState } from 'react';
import { Box, Collapse, Divider, Paper, Stack, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchRow from '@/views/home/matches/MatchRow';
import { EmojiEventsIcon, ExpandLessIcon, ExpandMoreIcon } from '@/views/core/MUI/icons/icons';

/** How many of a series' games a team has won so far. */
const winsFor = (matches: IMatchResponse[], teamName: string): number =>
  matches.filter(match => match.isFinished && match.winningTeamName === teamName).length;

/** A team's crest, looked up from any game it played — the series itself carries only names/ids. */
const logoFor = (matches: IMatchResponse[], teamId: GUID): string | undefined => {
  for (const match of matches) {
    if (match.homeTeam?.id === teamId) return match.homeTeam.logoUrl;
    if (match.visitorTeam?.id === teamId) return match.visitorTeam.logoUrl;
  }
  return undefined;
};

interface SeriesCardProps {
  series: IMatchSeriesResponse;
  /** This series' own games, in chronological order — numbered "Juego 1", "Juego 2", … */
  matches: IMatchResponse[];
  buildHref?: (match: IMatchResponse) => string;
}

/**
 * A best-of-N playoff series (HU-19): the matchup, who's ahead (a progress
 * bar toward the games needed to win), the winner once decided, and each
 * individual game listed as "Juego N" underneath — collapsible once the
 * headline result is visible, since a decided bo5/bo7 can list a lot of rows.
 */
export default function SeriesCard({ series, matches, buildHref }: SeriesCardProps) {
  const [expanded, setExpanded] = useState(true);

  const homeWins = winsFor(matches, series.homeTeamName);
  const visitorWins = winsFor(matches, series.visitorTeamName);
  const decided = series.winningTeamName != null;
  const leaderWins = Math.max(homeWins, visitorWins);
  const progress = series.bestOf > 0 ? Math.min(100, (leaderWins / series.bestOf) * 100) : 0;
  const barColor = decided ? 'success.main' : 'primary.main';

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 2,
        overflow: 'hidden',
      }}
    >
      <Box sx={{ p: 2 }}>
        <Stack
          direction="row"
          spacing={1}
          sx={{ alignItems: 'flex-start', justifyContent: 'space-between', flexWrap: 'wrap', rowGap: 1 }}
        >
          <Box sx={{ minWidth: 0 }}>
            <Typography
              variant="overline"
              sx={{ color: 'text.secondary', lineHeight: 1.4, display: 'block' }}
            >
              Serie
            </Typography>
            <Typography variant="subtitle1" component="p" sx={{ fontWeight: 700, lineHeight: 1.3 }}>
              {series.homeTeamName} vs {series.visitorTeamName}
            </Typography>
            <Typography variant="caption" sx={{ color: 'text.secondary' }}>
              Al mejor de {series.bestOf} · {matches.length} {matches.length === 1 ? 'juego' : 'juegos'}
            </Typography>
          </Box>

          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            {decided && (
              <Box sx={{ textAlign: 'right' }}>
                <Typography
                  variant="overline"
                  sx={{ color: 'text.secondary', lineHeight: 1.4, display: 'block' }}
                >
                  Ganador
                </Typography>
                <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', justifyContent: 'flex-end' }}>
                  <TeamLogo
                    teamName={series.winningTeamName!}
                    logoUrl={series.winningTeamId ? logoFor(matches, series.winningTeamId) : undefined}
                    size={22}
                  />
                  <Typography variant="body2" sx={{ fontWeight: 700, color: 'success.main' }}>
                    {series.winningTeamName}
                  </Typography>
                  <EmojiEventsIcon sx={{ fontSize: 16, color: 'success.main' }} />
                </Stack>
              </Box>
            )}
            <Box
              component="button"
              type="button"
              onClick={() => setExpanded(prev => !prev)}
              aria-expanded={expanded}
              aria-label={expanded ? 'Ocultar juegos de la serie' : 'Mostrar juegos de la serie'}
              sx={{
                display: 'flex',
                alignItems: 'center',
                p: 0.5,
                mt: -0.5,
                border: 'none',
                background: 'none',
                cursor: 'pointer',
                color: 'text.secondary',
                '&:hover': { color: 'text.primary' },
              }}
            >
              {expanded ? <ExpandLessIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
            </Box>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mt: 1.5 }}>
          <Typography
            variant="h6"
            sx={{ fontWeight: 700, minWidth: 20, textAlign: 'center', color: homeWins > visitorWins ? barColor : 'text.primary' }}
          >
            {homeWins}
          </Typography>
          <Box sx={{ flex: 1 }}>
            <Box
              sx={{
                height: 6,
                borderRadius: 3,
                bgcolor: 'action.hover',
                overflow: 'hidden',
              }}
            >
              <Box
                sx={{
                  height: '100%',
                  width: `${progress}%`,
                  bgcolor: barColor,
                  borderRadius: 3,
                  transition: 'width 0.2s ease',
                }}
              />
            </Box>
            <Typography
              variant="caption"
              sx={{ display: 'block', textAlign: 'center', color: 'text.secondary', mt: 0.5 }}
            >
              Serie {homeWins} - {visitorWins}
            </Typography>
          </Box>
          <Typography
            variant="h6"
            sx={{ fontWeight: 700, minWidth: 20, textAlign: 'center', color: visitorWins > homeWins ? barColor : 'text.primary' }}
          >
            {visitorWins}
          </Typography>
        </Stack>
      </Box>

      <Collapse in={expanded}>
        <Divider />
        <Stack divider={<Divider />}>
          {matches.map((match, index) => (
            <Box key={match.id}>
              <Typography
                variant="overline"
                sx={{ display: 'block', color: 'text.secondary', px: 2, pt: 1 }}
              >
                Juego {index + 1}
              </Typography>
              <MatchRow match={match} buildHref={buildHref} />
            </Box>
          ))}
        </Stack>
      </Collapse>
    </Paper>
  );
}
