import { useMemo } from 'react';
import { Box, Divider, Paper, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import MatchRow from '@/views/home/matches/MatchRow';
import { toArDayKey, formatArDayLabel } from '@/modules/core/utils/formatDate';

const dayKey = (value: string) => toArDayKey(value);

const formatRoundLabel = (key: string) => formatArDayLabel(key);

interface Round {
  key: string;
  label: string;
  matches: IMatchResponse[];
}

const groupByRound = (matches: IMatchResponse[]): Round[] => {
  const byKey = new Map<string, IMatchResponse[]>();

  matches.forEach(match => {
    const key = dayKey(match.matchDate);
    const group = byKey.get(key) ?? [];
    group.push(match);
    byKey.set(key, group);
  });

  return Array.from(byKey.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([key, roundMatches]) => ({
      key,
      label: formatRoundLabel(key),
      matches: [...roundMatches].sort((a, b) => a.matchDate.localeCompare(b.matchDate)),
    }));
};

export default function MatchFixtureList({ matches }: { matches: IMatchResponse[] }) {
  const rounds = useMemo(() => groupByRound(matches), [matches]);

  if (rounds.length === 0) return null;

  return (
    <Stack spacing={2.5}>
      {rounds.map(round => (
        <Box key={round.key}>
          <Typography
            variant="overline"
            sx={{
              color: "text.secondary",
              display: "block",
              mb: 1
            }}>
            {round.label}
          </Typography>
          <Paper variant="outlined">
            <Stack divider={<Divider />}>
              {round.matches.map(match => (
                <MatchRow key={match.id} match={match} />
              ))}
            </Stack>
          </Paper>
        </Box>
      ))}
    </Stack>
  );
}
