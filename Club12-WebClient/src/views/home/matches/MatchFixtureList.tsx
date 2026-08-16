import { useMemo } from 'react';
import { Box, Divider, Paper, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import MatchRow from '@/views/home/matches/MatchRow';

const dayKey = (value: string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? 'unknown' : parsed.toISOString().slice(0, 10);
};

const formatRoundLabel = (key: string) => {
  if (key === 'unknown') return 'Fecha a confirmar';
  const parsed = new Date(`${key}T00:00:00`);
  const label = parsed.toLocaleDateString('es-AR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });
  return label.charAt(0).toUpperCase() + label.slice(1);
};

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
            color="text.secondary"
            display="block"
            mb={1}
          >
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
