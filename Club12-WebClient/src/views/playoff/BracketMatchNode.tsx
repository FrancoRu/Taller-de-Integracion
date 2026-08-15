import { forwardRef } from 'react';
import { Box, Paper, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';

interface BracketMatchNodeProps {
  match: IMatchResponse;
}

type Participant = IMatchResponse['homeTeam'];

const teamLabel = (team: Participant): string => team?.name ?? 'A definir';

const isWinner = (match: IMatchResponse, teamId?: string | null): boolean =>
  Boolean(
    match.isFinished && match.winningTeamId && teamId && match.winningTeamId === teamId
  );

/**
 * A single bracket slot: home team on top, visitor team below. Shows the
 * recorded score when the match is finished and highlights the winning
 * side. Missing participants render as "A definir" (TBD).
 */
const BracketMatchNode = forwardRef<HTMLDivElement, BracketMatchNodeProps>(
  ({ match }, ref) => {
    const sides: Array<{ key: string; team: Participant }> = [
      { key: 'home', team: match.homeTeam },
      { key: 'visitor', team: match.visitorTeam },
    ];

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
                {teamLabel(team)}
              </Typography>
              {match.isFinished && team && (
                <Typography variant="body2" fontWeight={winner ? 700 : 500}>
                  {team.score}
                </Typography>
              )}
            </Box>
          );
        })}
      </Paper>
    );
  }
);

BracketMatchNode.displayName = 'BracketMatchNode';

export default BracketMatchNode;
