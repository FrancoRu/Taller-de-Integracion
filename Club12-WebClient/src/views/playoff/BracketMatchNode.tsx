import { Box, Paper, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import {
  bracketTeamLabel,
  isBracketMatchWinner,
  legGameScores,
  seriesGameScores,
} from '@/modules/playoff/matchStatus';
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
 * vuelta). The per-game scores themselves render as columns next to each
 * side's name (see `gameScoresForTeam`) — the badge is just the extra
 * context of how many games the format allows, since the column count
 * alone doesn't say whether a 2-0 series is already over (best-of-2) or
 * still has a decider left (best-of-3).
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
 * multi-leg tie — see `buildBracket.ts`), each side shows one score per
 * finished game/leg side by side (see `gameScoresForTeam`) instead of a
 * single aggregate, with a small format badge in the corner — see
 * `formatBadge`. A plain single match (no series, no legs) still shows just
 * the one final score, same as always.
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

  /**
   * One score per finished game/leg for `team`, in order — `null` when
   * there's no series/legs behind this match (a plain single game), so the
   * caller falls back to the one final `team.score` instead.
   */
  const gameScoresForTeam = (team: Participant): number[] | null => {
    if (!team) return null;
    if (series) return seriesGameScores(series, team.name);
    if (legs && legs.length > 1) return legGameScores(legs, team.id);
    return null;
  };

  return (
    <Paper
      variant="outlined"
      onClick={onClick}
      // Pure query hook, no visual effect — lets PlayoffBracket find this
      // card's rendered position in the DOM after mount, to hide a
      // dangling connector line coming from a bye sibling with no card of
      // its own (see PlayoffBracket's hideDanglingByeConnectors effect).
      data-match-id={match.id}
      sx={{
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
        // A real layout row, not an absolutely-positioned sticker: the
        // bracket library renders each card inside a nested <svg>/
        // <foreignObject> sized to exactly this card's box, which clips
        // overflow by default — a badge nudged outside the card's own
        // bounds (as this used to be) had its top sliced off on every
        // single best-of-N match. Taking real layout height instead means
        // it can never poke outside the box it's clipped to.
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', px: 0.75 }}>
          <Typography
            variant="caption"
            sx={{
              color: 'text.secondary',
              fontSize: '0.6rem',
              fontWeight: 600,
              lineHeight: 1.4,
              px: 0.4,
              borderRadius: 0.5,
              bgcolor: 'background.default',
              border: '1px solid',
              borderColor: 'divider',
            }}
          >
            {badge}
          </Typography>
        </Box>
      )}

      {sides.map(({ key, team }) => {
        const winner = isBracketMatchWinner(match, team?.id);
        const gameScores = gameScoresForTeam(team);

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
            {gameScores && gameScores.length > 0 ? (
              <Stack direction="row" spacing={0.5} sx={{ flexShrink: 0 }}>
                {gameScores.map((score, index) => (
                  <Typography
                    // A per-game score column has no id of its own; order IS
                    // its identity (game 1 is always first), so the array
                    // index is a stable, correct React key here.
                    key={index}
                    variant="body2"
                    sx={{ fontWeight: winner ? 700 : 500, minWidth: 16, textAlign: 'right' }}
                  >
                    {score}
                  </Typography>
                ))}
              </Stack>
            ) : (
              match.isFinished &&
              team && (
                <Typography variant="body2" sx={{ fontWeight: winner ? 700 : 500 }}>
                  {team.score}
                </Typography>
              )
            )}
          </Box>
        );
      })}
    </Paper>
  );
}
