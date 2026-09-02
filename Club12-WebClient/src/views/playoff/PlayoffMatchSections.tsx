import { Box, Paper, Stack, Typography } from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { DivisionFixtureSection } from '@/modules/match/utils/divisionFixtureSections';
import { groupFixtureSectionsByBracket } from '@/modules/match/utils/divisionFixtureSections';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import { EmojiEventsIcon } from '@/views/core/MUI/icons/icons';

interface PlayoffMatchSectionsProps {
  sections: DivisionFixtureSection[];
  seriesById?: Map<GUID, IMatchSeriesResponse>;
  /** Builds each match row's link target. Omit for the public match page (default). */
  buildHref?: (match: IMatchResponse) => string;
}

/**
 * A playoff division's real match list ("Partidos de playoff"), grouped by
 * cup (Copa Oro, Copa Plata, …) when the division splits its bracket that
 * way. Each cup renders as its own bordered card with a trophy-accented
 * title, so two cups' rounds read as clearly separate groups instead of one
 * flat list where nothing visually ties a cup's own rounds together — the
 * same border-and-title treatment already used for a single series' card,
 * just one level up. A division with a single, unnamed bracket renders its
 * rounds directly with no extra card wrapper, since there is only one group
 * and nothing to separate it from.
 */
export default function PlayoffMatchSections({
  sections,
  seriesById,
  buildHref,
}: PlayoffMatchSectionsProps) {
  const groups = groupFixtureSectionsByBracket(sections);

  const renderRounds = (roundSections: DivisionFixtureSection[]) => (
    <Stack spacing={3}>
      {roundSections.map(({ stage, label, matches: stageMatches }) => (
        <Box key={stage.id}>
          <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 1 }}>
            {label}
          </Typography>
          <MatchFixtureList
            matches={stageMatches}
            exportTitle={label}
            seriesById={seriesById}
            buildHref={buildHref}
          />
        </Box>
      ))}
    </Stack>
  );

  return (
    <Stack spacing={3}>
      {groups.map(group =>
        group.bracketName ? (
          <Paper
            key={group.bracketName}
            variant="outlined"
            sx={{ p: { xs: 2, sm: 2.5 }, borderRadius: 2 }}
          >
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 2.5 }}>
              <EmojiEventsIcon sx={{ color: 'primary.main', fontSize: 22 }} />
              <Typography variant="h6" sx={{ fontWeight: 700 }}>
                {group.bracketName}
              </Typography>
            </Stack>
            {renderRounds(group.sections)}
          </Paper>
        ) : (
          <Box key="default">{renderRounds(group.sections)}</Box>
        )
      )}
    </Stack>
  );
}
