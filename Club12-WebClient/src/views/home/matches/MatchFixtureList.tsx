import { useMemo, useState } from 'react';
import {
  Box,
  Chip,
  Collapse,
  Divider,
  Paper,
  Stack,
  Typography,
} from '@mui/material';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { GUID } from '@/modules/core/types/types';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { groupMatchesBySeries } from '@/modules/matchSeries/utils/groupMatchesBySeries';
import MatchRow from '@/views/home/matches/MatchRow';
import TeamLogo from '@/views/core/components/TeamLogo';
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
import { ExpandLessIcon, ExpandMoreIcon } from '@/views/core/MUI/icons/icons';

/** The round whose latest match time is closest to now (>= now first, else the last round). */
const nearestRoundKey = (
  rounds: { round: number | null; matches: IMatchResponse[] }[]
): number | null | undefined => {
  if (rounds.length === 0) return undefined;

  const now = Date.now();
  const repDate = (matches: IMatchResponse[]): number => {
    const times = matches
      .map(match => new Date(match.matchDate).getTime())
      .filter(time => !Number.isNaN(time));
    return times.length > 0 ? Math.max(...times) : 0;
  };

  const upcoming = rounds.find(round => repDate(round.matches) >= now);
  return (upcoming ?? rounds[rounds.length - 1]).round;
};

/**
 * A stage's fixture grouped by matchday (jornada, HU-63): the group header is
 * the round ("Fecha 1", "Fecha 2", …), NOT the calendar date — each match keeps
 * its own date/time inside its row. With an odd roster the team free that
 * matchday is shown as "Libre" (HU-65).
 *
 * Every fecha but the current/nearest one is collapsible (collapsed by
 * default) — a long group-stage fixture stayed fully expanded otherwise. The
 * current fecha (the first whose latest match is today or later, else the
 * last one) always stays open and has no toggle, so the thing you actually
 * came to look at is never hidden behind a click.
 */
export default function MatchFixtureList({
  matches,
  exportTitle,
  buildHref,
  seriesById,
}: {
  matches: IMatchResponse[];
  /**
   * When provided, shows an "Exportar CSV" button (HU-89) and uses this as the
   * download filename (e.g. the stage name). Omit to render the fixture with no
   * export affordance.
   */
  exportTitle?: string;
  /**
   * Builds each match row's link target. Omit to link to the public match page
   * (default); admin callers pass a builder pointing at the panel match page.
   */
  buildHref?: (match: IMatchResponse) => string;
  /**
   * A division's playoff series, keyed by id — when provided, a best-of-N
   * series' individual games are grouped under one shared header instead of
   * appearing as unrelated rows interleaved with other pairs' games under
   * the same stage (two series can play on overlapping dates). Omit for a
   * regular (non-playoff) fixture, where no match ever belongs to a series.
   */
  seriesById?: Map<GUID, IMatchSeriesResponse>;
}) {
  const rounds = useMemo(() => groupMatchesByRound(matches), [matches]);
  const stageTeamNames = useMemo(() => collectStageTeamNames(matches), [matches]);
  const currentRound = useMemo(() => nearestRoundKey(rounds), [rounds]);

  // A bye team is still a real team with its own escudo — it just isn't
  // playing this round. Look its logo up from any match it played elsewhere
  // in the stage, the same way its name is derived (byeTeamNamesForRound).
  const teamLogoByName = useMemo(() => {
    const logos = new Map<string, string | undefined>();
    matches.forEach(match => {
      if (match.homeTeam) logos.set(match.homeTeam.name, match.homeTeam.logoUrl);
      if (match.visitorTeam) logos.set(match.visitorTeam.name, match.visitorTeam.logoUrl);
    });
    return logos;
  }, [matches]);

  // Every collapsible (non-current) round starts collapsed; toggling adds/
  // removes it from this set. Keyed by round number, with `null` (knockout
  // matches with no jornada) normalized to a sentinel since Set can't
  // distinguish two `null`s from different renders by reference anyway.
  const [expandedRounds, setExpandedRounds] = useState<Set<number>>(new Set());
  const toggleRound = (round: number) => {
    setExpandedRounds(prev => {
      const next = new Set(prev);
      if (next.has(round)) {
        next.delete(round);
      } else {
        next.add(round);
      }
      return next;
    });
  };

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
        const roundKey = round.round ?? -1;
        const isCurrent = round.round === currentRound;
        const isExpanded = isCurrent || expandedRounds.has(roundKey);

        return (
          <Box key={round.round ?? 'knockout'}>
            {round.round == null ? null : isCurrent ? (
              // A knockout stage (null round) has exactly one group, always
              // shown expanded with no header of its own — the caller
              // already labels it (e.g. "Semifinal", "Final" above this
              // list), and the fallback "Fase final" text used to render
              // here for every single knockout round regardless of which
              // one it actually was, reading as the same phase repeating
              // over and over down the page.
              <Typography
                variant="overline"
                sx={{ color: 'text.secondary', display: 'block', mb: 1 }}
              >
                {formatRoundLabel(round.round)}
              </Typography>
            ) : (
              <Box
                component="button"
                type="button"
                onClick={() => toggleRound(roundKey)}
                aria-expanded={isExpanded}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 0.5,
                  width: '100%',
                  mb: 1,
                  p: 0,
                  border: 'none',
                  background: 'none',
                  cursor: 'pointer',
                  color: 'text.secondary',
                  '&:hover': { color: 'text.primary' },
                }}
              >
                <Typography variant="overline">
                  {formatRoundLabel(round.round)}
                </Typography>
                {isExpanded ? (
                  <ExpandLessIcon fontSize="small" />
                ) : (
                  <ExpandMoreIcon fontSize="small" />
                )}
              </Box>
            )}
            <Collapse in={isExpanded}>
              <Paper variant="outlined">
                <Stack divider={<Divider />}>
                  {(() => {
                    // A round-robin bye is derived purely from the roster diff
                    // (byeTeamNamesForRound) below — a fixture-generation slot
                    // with neither team assigned is corrupt data, not a real
                    // match, and must not render its own broken "—" vs "—" row.
                    const realMatches = round.matches.filter(
                      match => match.homeTeam || match.visitorTeam
                    );

                    if (!seriesById) {
                      return realMatches.map(match => (
                        <MatchRow key={match.id} match={match} buildHref={buildHref} />
                      ));
                    }

                    return groupMatchesBySeries(realMatches, seriesById).map(group => {
                      if (!group.series) {
                        const match = group.matches[0];
                        return <MatchRow key={match.id} match={match} buildHref={buildHref} />;
                      }

                      const { series } = group;
                      return (
                        <Box key={series.id}>
                          <Box sx={{ px: 2, pt: 1.5, pb: 0.5 }}>
                            <Typography variant="caption" sx={{ color: 'text.secondary', fontWeight: 600 }}>
                              Serie: {series.homeTeamName} vs {series.visitorTeamName} · Al
                              mejor de {series.bestOf}
                              {series.winningTeamName && ` · Ganó ${series.winningTeamName}`}
                            </Typography>
                          </Box>
                          <Stack divider={<Divider />}>
                            {group.matches.map(match => (
                              <MatchRow key={match.id} match={match} buildHref={buildHref} />
                            ))}
                          </Stack>
                        </Box>
                      );
                    });
                  })()}
                  {byes.map(teamName => (
                    <Box
                      key={`bye-${teamName}`}
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: '56px 1fr auto 1fr', sm: '56px 1fr auto 1fr 140px' },
                        alignItems: 'center',
                        gap: { xs: 1, sm: 2 },
                        px: 2,
                        py: 1.25,
                      }}
                    >
                      <Box />
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'flex-end', minWidth: 0 }}>
                        <TeamLogo teamName={teamName} logoUrl={teamLogoByName.get(teamName)} size={28} />
                        <Typography variant="body2" noWrap sx={{ fontWeight: 500, minWidth: 0 }}>
                          {teamName}
                        </Typography>
                      </Stack>
                      <Box sx={{ textAlign: 'center', minWidth: 56 }}>
                        <Chip label={BYE_TEAM_LABEL} size="small" variant="outlined" />
                      </Box>
                      <Box />
                    </Box>
                  ))}
                </Stack>
              </Paper>
            </Collapse>
          </Box>
        );
      })}
    </Stack>
  );
}
