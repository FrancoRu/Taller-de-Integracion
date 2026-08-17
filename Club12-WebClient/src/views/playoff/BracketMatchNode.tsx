import { Box, Paper, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { bracketTeamLabel, isBracketMatchWinner } from '@/modules/playoff/matchStatus';
import TeamLogo from '@/views/core/components/TeamLogo';

/** Pixel size of each side's `TeamLogo`, kept small so the card fits the bracket's fixed row height. */
const BRACKET_TEAM_LOGO_SIZE = 20;

interface BracketMatchNodeProps {
  match: IMatchResponse;
  /**
   * When this node represents a best-of-N series (BestOf > 1), the full
   * series data — enables showing the per-game breakdown below the
   * aggregate score.
   */
  series?: IMatchSeriesResponse;
}

type Participant = IMatchResponse['homeTeam'];

/**
 * Formats a series' finished games as a single-line, non-wrapping summary
 * (e.g. "J1 111-101 · J2 118-95"). Kept to one line — rather than an
 * expandable per-game list — because the bracket library reserves a fixed
 * height per match slot; a card that grows on interaction would overlap
 * its neighbors. The full text is also set as the `title` so a long
 * best-of-5/7 series is still fully readable on hover.
 */
const gameSummaryLine = (series: IMatchSeriesResponse): string =>
  series.games
    .filter(game => game.isFinished)
    .map(game => `J${game.gameNumber} ${game.homeScore}-${game.visitorScore}`)
    .join(' · ');

/**
 * A single bracket slot: home team on top, visitor team below, each with
 * its logo. Shows the recorded score when the match is finished and
 * highlights the winning side. A missing participant renders as "A
 * definir" (TBD) while the slot still awaits a previous round's winner, or
 * as "BYE" once the match is already decided with only one side ever
 * assigned (a seeding walkover). When `series` is provided (a best-of-N /
 * two-legged tie), the score shown is the aggregate game tally and a
 * compact one-line summary of each individual game is shown underneath.
 */
export default function BracketMatchNode({ match, series }: BracketMatchNodeProps) {
  const sides: Array<{ key: string; team: Participant }> = [
    { key: 'home', team: match.homeTeam },
    { key: 'visitor', team: match.visitorTeam },
  ];

  const summaryLine = series ? gameSummaryLine(series) : '';

  return (
    <Paper
      variant="outlined"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        gap: 0.5,
        p: 1,
        m: 0,
        width: '100%',
        height: '100%',
        minWidth: 200,
        borderRadius: 1.5,
        borderColor: 'divider',
        boxSizing: 'border-box',
      }}
    >
      {series && (
        <Typography variant="caption" sx={{ color: 'text.secondary', lineHeight: 1.2 }}>
          Al mejor de {series.bestOf}
        </Typography>
      )}

      {sides.map(({ key, team }) => {
        const winner = isBracketMatchWinner(match, team?.id);

        return (
          <Box
            key={key}
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              gap: 0.75,
              py: 0.25,
              px: 0.75,
              borderRadius: 1,
              bgcolor: winner ? 'rgba(255, 90, 31, 0.12)' : 'transparent',
            }}
          >
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', minWidth: 0 }}>
              <TeamLogo teamName={team?.name ?? '?'} logoUrl={team?.logoUrl} size={BRACKET_TEAM_LOGO_SIZE} />
              <Typography
                variant="body2"
                color={team ? 'text.primary' : 'text.secondary'}
                noWrap
                sx={{ fontWeight: winner ? 700 : 500, maxWidth: 120 }}
              >
                {bracketTeamLabel(team, match)}
              </Typography>
            </Stack>
            {match.isFinished && team && (
              <Typography variant="body2" sx={{ fontWeight: winner ? 700 : 500 }}>
                {team.score}
              </Typography>
            )}
          </Box>
        );
      })}

      {series && summaryLine && (
        <Typography
          variant="caption"
          title={summaryLine}
          noWrap
          sx={{ color: 'text.secondary', lineHeight: 1.2 }}
        >
          {summaryLine}
        </Typography>
      )}
    </Paper>
  );
}
