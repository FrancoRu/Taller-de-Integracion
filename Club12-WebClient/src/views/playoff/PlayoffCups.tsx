import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Stack,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import {
  DivisionFixtureSection,
  groupFixtureSectionsByBracket,
} from '@/modules/match/utils/divisionFixtureSections';
import { FIXTURE_CSV_HEADERS, buildFixtureCsvRows } from '@/modules/match/utils/matchFixtureCsv';
import { downloadCsv } from '@/modules/core/utils/csv';
import ExportCsvButton from '@/views/core/components/ExportCsvButton';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import PlayoffBracket from '@/views/playoff/PlayoffBracket';
import { EmojiEventsIcon, ExpandMoreIcon } from '@/views/core/MUI/icons/icons';

interface PlayoffCupsProps {
  bracketGroups: BracketGroup[];
  /** Every elimination-stage fixture section of the division (all cups combined). */
  matchSections: DivisionFixtureSection[];
  seriesById?: Map<GUID, IMatchSeriesResponse>;
  buildHref?: (match: IMatchResponse) => string;
  /** See {@link PlayoffBracket}'s `onMatchClick` — omitted in read-only contexts. */
  onMatchClick?: (matchId: GUID) => void;
}

/**
 * Renders a division's playoff: bracket + round-by-round match list for
 * each cup (Copa Oro, Copa Plata, …), one shared "Exportar CSV" per cup
 * covering every one of its rounds — not one button per round, which just
 * multiplied the same action across every Semifinal/Final section.
 *
 * When a division splits its bracket into several tiers, they are NOT
 * shown as equal-weight peers: the top cup (seeded from standings
 * position #1 — first in `bracketGroups`, see ChampionResolver on the
 * backend) is the division's real championship, with a trophy-icon
 * heading; every lower cup (Copa Plata, Copa Bronce, …) is a consolation
 * bracket for teams that didn't make the top group, so it gets a smaller,
 * muted heading — still an accordion so it CAN be collapsed to declutter,
 * but open by default (`defaultExpanded`), since nothing here should be
 * hidden without the reader choosing to hide it. A single-bracket division
 * (no named cups) renders with no tiering at all, identical to a division
 * that never had this distinction.
 */
export default function PlayoffCups({
  bracketGroups,
  matchSections,
  seriesById,
  buildHref,
  onMatchClick,
}: PlayoffCupsProps) {
  const hasBracketContent = bracketGroups.some(
    group => group.model.rounds.length > 0 || (group.model.thirdPlace?.matches.length ?? 0) > 0
  );

  if (!hasBracketContent) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        No hay fases de eliminación disponibles para esta división.
      </Typography>
    );
  }

  const fixturesByBracket = new Map(
    groupFixtureSectionsByBracket(matchSections).map(group => [group.bracketName, group.sections])
  );

  const renderCupContent = (group: BracketGroup) => {
    const sections = fixturesByBracket.get(group.bracketName) ?? [];
    const allMatches = sections.flatMap(section => section.matches);

    return (
      <Stack spacing={3}>
        <PlayoffBracket model={group.model} seriesById={seriesById} onMatchClick={onMatchClick} />

        {sections.length > 0 && (
          <Box>
            <Stack
              direction="row"
              spacing={2}
              sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2 }}
            >
              <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                Partidos
              </Typography>
              <ExportCsvButton
                onExport={() =>
                  downloadCsv(
                    `fixture-playoff${group.bracketName ? `-${group.bracketName}` : ''}`,
                    FIXTURE_CSV_HEADERS,
                    buildFixtureCsvRows(allMatches)
                  )
                }
              />
            </Stack>
            <Stack spacing={3}>
              {sections.map(({ stage, label, matches: stageMatches }) => (
                <Box key={stage.id}>
                  <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 1 }}>
                    {label}
                  </Typography>
                  <MatchFixtureList matches={stageMatches} seriesById={seriesById} buildHref={buildHref} />
                </Box>
              ))}
            </Stack>
          </Box>
        )}
      </Stack>
    );
  };

  if (bracketGroups.length <= 1) {
    return renderCupContent(bracketGroups[0]);
  }

  const [topCup, ...otherCups] = bracketGroups;

  return (
    <Stack spacing={4}>
      <Box>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2.5 }}>
          <EmojiEventsIcon sx={{ color: 'primary.main', fontSize: 24 }} />
          <Typography variant="h6" sx={{ fontWeight: 700 }}>
            {topCup.bracketName}
          </Typography>
        </Stack>
        {renderCupContent(topCup)}
      </Box>

      <Stack spacing={1.5}>
        {otherCups.map(group => (
          <Accordion
            key={group.bracketName ?? 'default'}
            variant="outlined"
            disableGutters
            defaultExpanded
            sx={{
              borderRadius: 2,
              '&:before': { display: 'none' },
              '&.Mui-expanded': { margin: 0 },
            }}
          >
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="subtitle1" sx={{ color: 'text.secondary', fontWeight: 600 }}>
                {group.bracketName}
              </Typography>
            </AccordionSummary>
            <AccordionDetails sx={{ pt: 1 }}>{renderCupContent(group)}</AccordionDetails>
          </Accordion>
        ))}
      </Stack>
    </Stack>
  );
}
