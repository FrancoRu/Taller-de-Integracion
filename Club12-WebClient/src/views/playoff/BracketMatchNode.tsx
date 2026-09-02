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
   * series data — used only for the small "BOn" corner badge (see
   * `formatBadge`); the per-game breakdown lives in `SeriesCard` instead.
   */
  series?: IMatchSeriesResponse;
  /**
   * When this node aggregates more than one raw `Match` row between the
   * same two teams (e.g. a historical home-and-away tie with no
   * `MatchSeries` behind it — see `buildBracket.ts`'s tie grouping), the
   * individual legs in chronological order — used only for the corner
   * badge. Mutually exclusive with `series` in practice: a node is either
   * an admin-defined series or a client-inferred tie, never both.
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
 * A short corner badge naming the format (e.g. "BO3", "IV" for ida y
 * vuelta) — NOT a per-game breakdown. Every individual game/leg's score is
 * already shown in full, per game, in the "Partidos de playoff" list next
 * to the bracket (`SeriesCard`), so cramming that same detail into the
 * bracket card too only inflated it — a plain single-game match and a
 * best-of-N series card ended up wildly different heights even though the
 * library gives every round the same fixed box. The badge is absolutely
 * positioned (adds no layout height of its own), so a series card is now
 * exactly as tall as a plain one.
 */
const formatBadge = (series: IMatchSeriesResponse | undefined, legs: IMatchResponse[] | undefined): string | null => {
  if (series) return `BO${series.bestOf}`;
  if (legs && legs.length > 1) return legs.length === 2 ? 'IV' : `${legs.length}P`;
  return null;
};

/**
 * A single bracket slot: home team on top, visitor team below, each with
 * its logo. Shows the recorded score when the match is finished and
 * highlights the winning side. A missing participant renders as "A
 * definir" (TBD) while the slot still awaits a previous round's winner, or
 * as "BYE" once the match is already decided with only one side ever
 * assigned (a seeding walkover). When `series` is provided (an
 * admin-defined best-of-N series) or `legs` is provided (a client-inferred
 * multi-leg tie — see `buildBracket.ts`), the score shown is the aggregate
 * tally, with a small format badge in the corner — see `formatBadge`.
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

  const badge = formatBadge(series, legs);

  return (
    <Paper
      variant="outlined"
      onClick={onClick}
      sx={{
        position: 'relative',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        gap: 0.4,
        p: 0.75,
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
      {badge && (
        <Box
          sx={{
            position: 'absolute',
            // Nudged half outside the card's own top-right corner (rather
            // than flush inside it) so it reads as a tag hanging off the
            // card instead of crowding the first row's score, which sits
            // in that same corner.
            top: -6,
            right: -4,
            px: 0.4,
            borderRadius: 0.5,
            bgcolor: 'background.default',
            border: '1px solid',
            borderColor: 'divider',
            lineHeight: 1.4,
          }}
        >
          <Typography variant="caption" sx={{ color: 'text.secondary', fontSize: '0.6rem', fontWeight: 600 }}>
            {badge}
          </Typography>
        </Box>
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
            <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', minWidth: 0, flex: 1 }}>
              <TeamLogo teamName={team?.name ?? '?'} logoUrl={team?.logoUrl} size={BRACKET_TEAM_LOGO_SIZE} />
              <Typography
                variant="body2"
                color={team ? 'text.primary' : 'text.secondary'}
                noWrap
                title={bracketTeamLabel(team, match)}
                // Grows to fill whatever room the card actually has (minus
                // the crest and the score) instead of a fixed width picked
                // for an old, narrower card size — that hardcoded width
                // truncated most real team names ("Independiente de…") even
                // though the card had plenty of unused space to the right.
                sx={{ fontWeight: winner ? 700 : 500, minWidth: 0 }}
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
    </Paper>
  );
}
