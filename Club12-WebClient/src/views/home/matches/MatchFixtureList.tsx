import { useMemo } from 'react';
import { Box, Chip, Divider, Paper, Stack, Typography } from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import MatchRow from '@/views/home/matches/MatchRow';
import {
  BYE_TEAM_LABEL,
  byeTeamNamesForRound,
  collectStageTeamNames,
  formatRoundLabel,
  groupMatchesByRound,
} from '@/modules/match/utils/matchGrouping';
import {
  FIXTURE_CSV_HEADERS,
  buildFixtureCsvRows,
} from '@/modules/match/utils/matchFixtureCsv';
import { downloadCsv } from '@/modules/core/utils/csv';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';

/**
 * A stage's fixture grouped by matchday (jornada, HU-63): the group header is
 * the round ("Fecha 1", "Fecha 2", …), NOT the calendar date — each match keeps
 * its own date/time inside its row. With an odd roster the team free that
 * matchday is shown as "Libre" (HU-65).
 */
export default function MatchFixtureList({
  matches,
  exportTitle,
}: {
  matches: IMatchResponse[];
  /**
   * When provided, shows an "Exportar CSV" button (HU-89) and uses this as the
   * download filename (e.g. the stage name). Omit to render the fixture with no
   * export affordance.
   */
  exportTitle?: string;
}) {
  const rounds = useMemo(() => groupMatchesByRound(matches), [matches]);
  const stageTeamNames = useMemo(() => collectStageTeamNames(matches), [matches]);

  if (rounds.length === 0) return null;

  const handleExportCsv = () =>
    downloadCsv(
      exportTitle ? `fixture-${exportTitle}` : 'fixture',
      FIXTURE_CSV_HEADERS,
      buildFixtureCsvRows(matches)
    );

  return (
    <Stack spacing={2.5}>
      {exportTitle && (
        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
          <ExportCsvButton onExport={handleExportCsv} />
        </Box>
      )}
      {rounds.map(round => {
        const byes = byeTeamNamesForRound(round.matches, stageTeamNames);

        return (
          <Box key={round.round ?? 'knockout'}>
            <Typography
              variant="overline"
              sx={{
                color: 'text.secondary',
                display: 'block',
                mb: 1,
              }}
            >
              {formatRoundLabel(round.round)}
            </Typography>
            <Paper variant="outlined">
              <Stack divider={<Divider />}>
                {round.matches.map(match => (
                  <MatchRow key={match.id} match={match} />
                ))}
                {byes.map(teamName => (
                  <Stack
                    key={`bye-${teamName}`}
                    direction="row"
                    spacing={1}
                    sx={{ alignItems: 'center', px: 2, py: 1.25 }}
                  >
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {teamName}
                    </Typography>
                    <Chip label={BYE_TEAM_LABEL} size="small" variant="outlined" />
                  </Stack>
                ))}
              </Stack>
            </Paper>
          </Box>
        );
      })}
    </Stack>
  );
}
