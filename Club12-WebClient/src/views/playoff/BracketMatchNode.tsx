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
  /**
   * When this node aggregates more than one raw `Match` row between the
   * same two teams (e.g. a historical home-and-away tie with no
   * `MatchSeries` behind it — see `buildBracket.ts`'s tie grouping), the
   * individual legs in chronological order — enables showing a per-leg
   * breakdown below the aggregate score. Mutually exclusive with
   * `series` in practice: a node is either an admin-defined series or a
   * client-inferred tie, never both.
   */
  legs?: IMatchResponse[];
  /**
   * When provided, the whole card becomes clickable (e.g. the admin panel
   * wires this to open the match's full detail/edit page). Omitted in
   * read-only contexts like the public division page.
   */
  onClick?: () => void;
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
 * Formats a multi-leg tie's finished legs as a single-line, non-wrapping
 * summary (e.g. "P1 41-64 · P2 57-54") — the raw score as it was recorded
 * on each leg (home/visitor may swap between legs), mirroring
 * {@link gameSummaryLine}'s one-line convention for the same fixed-height
 * card. Numbered by chronological position, not by filtered index, so a
 * still-unfinished middle leg doesn't shift later legs' numbers.
 */
const legSummaryLine = (legs: IMatchResponse[]): string =>
  legs
    .map((leg, index) => ({ leg, index }))
    .filter(({ leg }) => leg.isFinished && leg.homeTeam && leg.visitorTeam)
    .map(({ leg, index }) => `P${index + 1} ${leg.homeTeam!.score}-${leg.visitorTeam!.score}`)
    .join(' · ');

/**
 * The caption shown above a tie's aggregate score. "Ida y vuelta" (the
 * conventional Spanish term for a two-legged home-and-away tie) for the
 * common two-leg case; a generic leg count otherwise, since grouping
 * doesn't assume exactly two legs.
 */
const tieCaption = (legCount: number): string =>
  legCount === 2 ? 'Ida y vuelta' : `${legCount} partidos`;

/**
 * A single bracket slot: home team on top, visitor team below, each with
 * its logo. Shows the recorded score when the match is finished and
 * highlights the winning side. A missing participant renders as "A
 * definir" (TBD) while the slot still awaits a previous round's winner, or
 * as "BYE" once the match is already decided with only one side ever
 * assigned (a seeding walkover). When `series` is provided (an
 * admin-defined best-of-N series) or `legs` is provided (a client-inferred
 * multi-leg tie — see `buildBracket.ts`), the score shown is the aggregate
 * tally and a compact one-line summary of each individual game/leg is
 * shown underneath.
 */
export default function BracketMatchNode({
  match,
  series,
  legs,
  onClick,
}: BracketMatchNodeProps) {
  const sides: Array<{ key: string; team: Participant }> = [
    { key: 'home', team: match.homeTeam },
    { key: 'visitor', team: match.visitorTeam },
  ];

  const isTie = !series && Boolean(legs && legs.length > 1);
  const summaryLine = series ? gameSummaryLine(series) : isTie ? legSummaryLine(legs!) : '';

  return (
    <Paper
      variant="outlined"
      onClick={onClick}
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
        cursor: onClick ? 'pointer' : 'default',
        ...(onClick && {
          '&:hover': { borderColor: 'primary.main' },
        }),
      }}
    >
      {series && (
        <Typography variant="caption" sx={{ color: 'text.secondary', lineHeight: 1.2 }}>
          Al mejor de {series.bestOf}
        </Typography>
      )}

      {isTie && (
        <Typography variant="caption" sx={{ color: 'text.secondary', lineHeight: 1.2 }}>
          {tieCaption(legs!.length)}
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

      {(series || isTie) && summaryLine && (
        <Typography
          variant="caption"
          title={summaryLine}
          sx={{
            color: 'text.secondary',
            lineHeight: 1.2,
            // Every finished game must stay visible (a decided bo7 can have
            // up to 7) — wrap onto 2 lines instead of truncating to one, and
            // only clamp/ellipsize past that as a last resort. `title` still
            // carries the full text for the rare case it doesn't fit.
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical',
            overflow: 'hidden',
          }}
        >
          {summaryLine}
        </Typography>
      )}
    </Paper>
  );
}
