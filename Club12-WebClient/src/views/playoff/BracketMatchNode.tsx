import { forwardRef, useState } from 'react';
import { Box, Collapse, IconButton, Paper, Stack, Typography } from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';

interface BracketMatchNodeProps {
  match: IMatchResponse;
  /**
   * When this node represents a best-of-N series (BestOf > 1), the full
   * series data — enables an expandable per-game breakdown below the
   * aggregate score.
   */
  series?: IMatchSeriesResponse;
}

type Participant = IMatchResponse['homeTeam'];

/**
 * A match is a walkover (bye) when it was seeded with only one side
 * present and is already finished — the other side never had an
 * opponent, as opposed to a not-yet-seeded slot still waiting on a
 * previous round's winner.
 */
const isBye = (match: IMatchResponse): boolean =>
  match.isFinished && Boolean(match.homeTeam) !== Boolean(match.visitorTeam);

const teamLabel = (team: Participant, match: IMatchResponse): string => {
  if (team) return team.name;
  return isBye(match) ? 'BYE' : 'A definir';
};

const isWinner = (match: IMatchResponse, teamId?: string | null): boolean =>
  Boolean(
    match.isFinished && match.winningTeamId && teamId && match.winningTeamId === teamId
  );

/**
 * A single bracket slot: home team on top, visitor team below. Shows the
 * recorded score when the match is finished and highlights the winning
 * side. A missing participant renders as "A definir" (TBD) while the slot
 * still awaits a previous round's winner, or as "BYE" once the match is
 * already decided with only one side ever assigned (a seeding walkover).
 * When `series` is provided, the score shown is the aggregate game tally
 * and the node can be expanded to see each individual game.
 */
const BracketMatchNode = forwardRef<HTMLDivElement, BracketMatchNodeProps>(
  ({ match, series }, ref) => {
    const [expanded, setExpanded] = useState(false);

    const sides: Array<{ key: string; team: Participant }> = [
      { key: 'home', team: match.homeTeam },
      { key: 'visitor', team: match.visitorTeam },
    ];

    const hasGames = Boolean(series && series.games.length > 0);

    return (
      <Paper
        ref={ref}
        variant="outlined"
        sx={{
          p: 1,
          m: 0,
          minWidth: 200,
          borderRadius: 1.5,
          borderColor: 'divider',
        }}
      >
        {series && (
          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 0.5 }}>
            <Typography variant="caption" color="text.secondary">
              Al mejor de {series.bestOf}
            </Typography>
            {hasGames && (
              <IconButton
                size="small"
                aria-label={expanded ? 'Ocultar partidos' : 'Ver partidos'}
                onClick={() => setExpanded(prev => !prev)}
                sx={{
                  transform: expanded ? 'rotate(180deg)' : 'none',
                  transition: 'transform 0.15s',
                }}
              >
                <ExpandMoreIcon fontSize="small" />
              </IconButton>
            )}
          </Stack>
        )}

        {sides.map(({ key, team }) => {
          const winner = isWinner(match, team?.id);

          return (
            <Box
              key={key}
              display="flex"
              justifyContent="space-between"
              alignItems="center"
              sx={{
                py: 0.5,
                px: 0.75,
                borderRadius: 1,
                bgcolor: winner ? 'rgba(255, 90, 31, 0.12)' : 'transparent',
              }}
            >
              <Typography
                variant="body2"
                fontWeight={winner ? 700 : 500}
                color={team ? 'text.primary' : 'text.secondary'}
                noWrap
                sx={{ maxWidth: 140 }}
              >
                {teamLabel(team, match)}
              </Typography>
              {match.isFinished && team && (
                <Typography variant="body2" fontWeight={winner ? 700 : 500}>
                  {team.score}
                </Typography>
              )}
            </Box>
          );
        })}

        {series && hasGames && (
          <Collapse in={expanded}>
            <Stack spacing={0.5} sx={{ mt: 1, pt: 1, borderTop: 1, borderColor: 'divider' }}>
              {series.games.map(game => (
                <Stack key={game.id} direction="row" justifyContent="space-between">
                  <Typography variant="caption" color="text.secondary">
                    Juego {game.gameNumber}
                  </Typography>
                  <Typography variant="caption">
                    {game.isFinished ? `${game.homeScore} - ${game.visitorScore}` : 'Pendiente'}
                  </Typography>
                </Stack>
              ))}
            </Stack>
          </Collapse>
        )}
      </Paper>
    );
  }
);

BracketMatchNode.displayName = 'BracketMatchNode';

export default BracketMatchNode;
