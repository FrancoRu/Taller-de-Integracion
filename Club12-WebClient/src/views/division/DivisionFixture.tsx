import { useEffect, useMemo, useState } from 'react';
import { Box, Typography } from '@mui/material';
import { ListSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { IStageResponse } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';
import SectionHeading from '@/views/core/components/SectionHeading';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';

const FETCH_PAGE_SIZE = 100;

interface DivisionFixtureProps {
  divisionId: GUID;
  divisionName: string;
  /**
   * Builds each match row's link target. Omit to link to the public match page
   * (default); admin callers pass a builder pointing at the panel match page.
   */
  buildMatchHref?: (match: IMatchResponse) => string;
}

/**
 * A division's fixture grouped by stage → jornada ("Fecha N"), rendered as
 * {@link MatchFixtureList} sections. Shared by the public division panel and the
 * admin division page; the admin passes `buildMatchHref` to route rows to the
 * panel match page instead of the public one.
 */
export default function DivisionFixture({
  divisionId,
  divisionName,
  buildMatchHref,
}: DivisionFixtureProps) {
  const [stages, setStages] = useState<IStageResponse[]>([]);
  const [matches, setMatches] = useState<IMatchResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    const fetchFixture = async () => {
      setLoading(true);
      try {
        const [stagesResponse, matchesResponse] = await Promise.all([
          stageService.getStagesByFilters({ divisionId, pageSize: FETCH_PAGE_SIZE }),
          matchService.getMatchByFilter({ divisionId, pageSize: FETCH_PAGE_SIZE }),
        ]);
        if (!cancelled) {
          setStages(stagesResponse.data?.items ?? []);
          setMatches(matchesResponse.data?.items ?? []);
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    void fetchFixture();
    return () => {
      cancelled = true;
    };
  }, [divisionId]);

  const sections = useMemo(
    () => buildDivisionFixtureSections(stages, matches, divisionName),
    [stages, matches, divisionName]
  );

  if (loading) return <ListSkeleton items={6} />;

  if (sections.length === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        No hay partidos registrados en esta división.
      </Typography>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {sections.map(({ stage, label, matches: stageMatches }) => (
        <Box key={stage.id}>
          <SectionHeading>{label}</SectionHeading>
          <MatchFixtureList matches={stageMatches} exportTitle={label} buildHref={buildMatchHref} />
        </Box>
      ))}
    </Box>
  );
}
