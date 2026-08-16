import { useRef } from 'react';
import { Box, Stack, Typography } from '@mui/material';
import { BracketModel } from '@/modules/playoff/type/bracket.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { translateStageType } from '@/modules/core/utils/translateStageType';
import BracketMatchNode from '@/views/playoff/BracketMatchNode';
import BracketConnectors from '@/views/playoff/BracketConnectors';

interface PlayoffBracketProps {
  model: BracketModel;
  /**
   * Maps a bracket node's id to its full series data (only relevant for
   * BestOf > 1 rounds, where the node id is the MatchSeries id) so
   * BracketMatchNode can show the per-game breakdown.
   */
  seriesById?: Map<GUID, IMatchSeriesResponse>;
}

/**
 * Renders one division's elimination bracket: round columns left-to-right
 * (Cuartos -> Semifinal -> Final), the Final as the rightmost/terminal
 * node, ThirdPlace as a side match beside it, and SVG connectors inferred
 * client-side from `model.edges`.
 */
export default function PlayoffBracket({ model, seriesById }: PlayoffBracketProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const nodeRefs = useRef<Map<GUID, HTMLDivElement | null>>(new Map());

  const hasThirdPlace = Boolean(model.thirdPlace && model.thirdPlace.matches.length > 0);
  const isEmpty = model.rounds.length === 0 && !hasThirdPlace;

  if (isEmpty) {
    return (
      <Typography color="text.secondary">
        No hay fases de eliminación disponibles para esta división.
      </Typography>
    );
  }

  return (
    <Box ref={containerRef} sx={{ position: 'relative', overflowX: 'auto', py: 2 }}>
      <Stack direction="row" spacing={5} alignItems="flex-start">
        {model.rounds.map(round => (
          <Stack key={round.stageId} spacing={3} sx={{ minWidth: 220 }}>
            <Typography
              variant="subtitle2"
              component="h3"
              sx={{
                fontFamily: "'Oswald', sans-serif",
                textTransform: 'uppercase',
                letterSpacing: '0.04em',
                color: 'secondary.main',
              }}
            >
              {translateStageType(round.stageType)}
            </Typography>
            <Stack spacing={4}>
              {round.matches.map(match => (
                <BracketMatchNode
                  key={match.id}
                  match={match}
                  series={seriesById?.get(match.id)}
                  ref={node => nodeRefs.current.set(match.id, node)}
                />
              ))}
            </Stack>
          </Stack>
        ))}

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
              {model.thirdPlace.matches.map(match => (
                <BracketMatchNode
                  key={match.id}
                  match={match}
                  series={seriesById?.get(match.id)}
                  ref={node => nodeRefs.current.set(match.id, node)}
                />
              ))}
            </Stack>
          </Stack>
        )}
      </Stack>

      <BracketConnectors edges={model.edges} containerRef={containerRef} nodeRefs={nodeRefs} />
    </Box>
  );
}
