import { useMemo } from 'react';
import { Box, Stack, Typography } from '@mui/material';
import { SingleEliminationBracket } from '@g-loot/react-tournament-brackets';
import type { LibraryMatchComponentProps } from '@/modules/playoff/type/gLootBracketTypes.d';
import { BracketModel } from '@/modules/playoff/type/bracket.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { toLibraryMatches, libraryRoundLabels } from '@/modules/playoff/bracketAdapter';
import { resolveClickTargetMatchId } from '@/modules/playoff/bracketMatchNavigation';
import BracketMatchNode from '@/views/playoff/BracketMatchNode';
import BracketMatchLibraryAdapter from '@/views/playoff/BracketMatchLibraryAdapter';
import {
  PLAYOFF_BRACKET_BOX_HEIGHT,
  PLAYOFF_BRACKET_OPTIONS,
  PLAYOFF_BRACKET_THEME,
} from '@/views/playoff/playoffBracketTheme';

interface PlayoffBracketProps {
  model: BracketModel;
  /**
   * Maps a bracket node's id to its full series data (only relevant for
   * BestOf > 1 rounds, where the node id is the MatchSeries id) so
   * BracketMatchNode can show the per-game breakdown.
   */
  seriesById?: Map<GUID, IMatchSeriesResponse>;
  /** See {@link resolveClickTargetMatchId} — omitted in read-only contexts. */
  onMatchClick?: (matchId: GUID) => void;
}

/**
 * Renders one division's elimination bracket using
 * `@g-loot/react-tournament-brackets` for round layout and connector
 * lines (Cuartos -> Semifinal -> Final), with the ThirdPlace match shown
 * as an unconnected side slot beside the Final. `BracketMatchLibraryAdapter`
 * swaps in this app's own `BracketMatchNode` card (team logos, best-of-N
 * series breakdown, dark theme) in place of the library's default look.
 */
export default function PlayoffBracket({
  model,
  seriesById,
  onMatchClick,
}: PlayoffBracketProps) {
  const matches = useMemo(() => toLibraryMatches(model), [model]);
  const roundLabels = useMemo(() => libraryRoundLabels(model), [model]);

  const hasThirdPlace = Boolean(model.thirdPlace && model.thirdPlace.matches.length > 0);
  const isEmpty = matches.length === 0 && !hasThirdPlace;

  if (isEmpty) {
    return (
      <Typography sx={{
        color: "text.secondary"
      }}>No hay fases de eliminación disponibles para esta división.
              </Typography>
    );
  }

  const matchComponent = ({ match }: LibraryMatchComponentProps) => (
    <BracketMatchLibraryAdapter
      match={match}
      seriesById={seriesById}
      onMatchClick={onMatchClick}
    />
  );

  return (
    <Box sx={{ overflowX: 'auto', py: 2 }}>
      <Stack direction="row" spacing={5} sx={{ alignItems: 'flex-start' }}>
        {matches.length > 0 && (
          <Box>
            <SingleEliminationBracket
              matches={matches}
              matchComponent={matchComponent}
              theme={PLAYOFF_BRACKET_THEME}
              options={{
                style: {
                  ...PLAYOFF_BRACKET_OPTIONS,
                  roundHeader: {
                    ...PLAYOFF_BRACKET_OPTIONS.roundHeader,
                    roundTextGenerator: (roundNumber: number) => roundLabels[roundNumber - 1],
                  },
                },
              }}
              svgWrapper={({ children, bracketWidth, bracketHeight }) => (
                <Box sx={{ width: bracketWidth, height: bracketHeight }}>{children}</Box>
              )}
            />
          </Box>
        )}

        {hasThirdPlace && model.thirdPlace && (
          <Stack spacing={3} sx={{ minWidth: 220, alignSelf: 'flex-end' }}>
            <Typography
              variant="subtitle2"
              component="h3"
              sx={{
                fontFamily: "'Oswald', sans-serif",
                textTransform: 'uppercase',
                letterSpacing: '0.04em',
                color: 'text.secondary',
              }}
            >
              Tercer puesto
            </Typography>
            <Stack spacing={2}>
              {model.thirdPlace.matches.map(match => {
                const series = seriesById?.get(match.id);
                const legs = model.thirdPlace?.legsByMatchId?.get(match.id);
                const targetId = onMatchClick
                  ? resolveClickTargetMatchId(match, series, legs)
                  : undefined;

                return (
                  <Box key={match.id} sx={{ height: PLAYOFF_BRACKET_BOX_HEIGHT }}>
                    <BracketMatchNode
                      match={match}
                      series={series}
                      legs={legs}
                      onClick={targetId ? () => onMatchClick!(targetId) : undefined}
                    />
                  </Box>
                );
              })}
            </Stack>
          </Stack>
        )}
      </Stack>
    </Box>
  );
}
