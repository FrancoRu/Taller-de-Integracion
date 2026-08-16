import { Divider, Stack, Typography } from '@mui/material';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import PlayoffBracket from '@/views/playoff/PlayoffBracket';

interface PlayoffBracketsProps {
  groups: BracketGroup[];
  seriesById?: Map<GUID, IMatchSeriesResponse>;
}

/**
 * Renders every named bracket of a division side by side (stacked
 * vertically, each with its own labeled section). A division with a
 * single, unnamed elimination path renders as one section with no
 * heading — multiple brackets only appear when the admin actually named
 * more than one (e.g. two parallel cups within the same division).
 */
export default function PlayoffBrackets({ groups, seriesById }: PlayoffBracketsProps) {
  const hasContent = groups.some(
    group => group.model.rounds.length > 0 || (group.model.thirdPlace?.matches.length ?? 0) > 0
  );

  if (!hasContent) {
    return (
      <Typography color="text.secondary">
        No hay fases de eliminación disponibles para esta división.
      </Typography>
    );
  }

  const showLabels = groups.length > 1;

  return (
    <Stack spacing={4} divider={showLabels ? <Divider /> : undefined}>
      {groups.map(group => (
        <Stack key={group.bracketName ?? 'default'} spacing={1.5}>
          {showLabels && (
            <Typography
              variant="subtitle1"
              component="h3"
              sx={{
                fontFamily: "'Oswald', sans-serif",
                textTransform: 'uppercase',
                letterSpacing: '0.04em',
              }}
            >
              {group.bracketName}
            </Typography>
          )}
          <PlayoffBracket model={group.model} seriesById={seriesById} />
        </Stack>
      ))}
    </Stack>
  );
}
