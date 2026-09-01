import { useEffect, useMemo, useState } from 'react';
import { Box, Button, Stack, Typography } from '@mui/material';
import { ListSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { IStageResponse } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';
import {
  formatRoundLabel,
  groupMatchesByRound,
} from '@/modules/match/utils/matchGrouping';
import SectionHeading from '@/views/core/components/SectionHeading';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import { ArrowBackIcon, ArrowForwardIcon } from '@/views/core/MUI/icons/icons';

const FETCH_PAGE_SIZE = 100;

interface DivisionFixtureProps {
  divisionId: GUID;
  divisionName: string;
  /**
   * 'stacked' (default) renders every fecha at once — used by the public panel.
   * 'carousel' shows one fecha (jornada) at a time with prev/next controls,
   * defaulting to the fecha closest to today — used by the admin division page.
   */
  variant?: 'stacked' | 'carousel';
  /**
   * Builds each match row's link target. Omit to link to the public match page
   * (default); admin callers pass a builder pointing at the panel match page.
   */
  buildMatchHref?: (match: IMatchResponse) => string;
}

/** One carousel slide: a single fecha (jornada) within a stage. */
interface FixtureSlide {
  key: string;
  stageLabel: string;
  matches: IMatchResponse[];
  /** Latest match time in the fecha, used to default to the closest fecha. */
  repDate: number;
}

/**
 * A division's fixture grouped by stage → jornada ("Fecha N"). The public panel
 * renders every fecha stacked ('stacked'); the admin division page renders one
 * fecha at a time as a carousel ('carousel'). The admin passes `buildMatchHref`
 * to route rows to the panel match page instead of the public one.
 */
export default function DivisionFixture({
  divisionId,
  divisionName,
  variant = 'stacked',
  buildMatchHref,
}: DivisionFixtureProps) {
  const [stages, setStages] = useState<IStageResponse[]>([]);
  const [matches, setMatches] = useState<IMatchResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [index, setIndex] = useState(0);

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

  // Flatten the stage sections into one ordered list of fechas (jornadas) for
  // the carousel; each round of each stage becomes a slide.
  const slides = useMemo<FixtureSlide[]>(() => {
    const result: FixtureSlide[] = [];
    sections.forEach(section => {
      groupMatchesByRound(section.matches).forEach(round => {
        const times = round.matches
          .map(match => new Date(match.matchDate).getTime())
          .filter(time => !Number.isNaN(time));
        result.push({
          key: `${section.stage.id}-${round.round ?? 'ko'}`,
          stageLabel: section.label,
          matches: round.matches,
          repDate: times.length > 0 ? Math.max(...times) : 0,
        });
      });
    });
    return result;
  }, [sections]);

  const multiStage = useMemo(
    () => new Set(slides.map(slide => slide.stageLabel)).size > 1,
    [slides]
  );

  // Default the carousel to the fecha closest to today: the first whose latest
  // match is today or later, else the last one.
  useEffect(() => {
    if (slides.length === 0) return;
    const now = Date.now();
    const upcoming = slides.findIndex(slide => slide.repDate >= now);
    setIndex(upcoming >= 0 ? upcoming : slides.length - 1);
  }, [slides]);

  if (loading) return <ListSkeleton items={6} />;

  if (sections.length === 0) {
    return (
      <Typography sx={{ color: 'text.secondary' }}>
        No hay partidos registrados en esta división.
      </Typography>
    );
  }

  if (variant === 'carousel') {
    if (slides.length === 0) {
      return (
        <Typography sx={{ color: 'text.secondary' }}>
          No hay partidos registrados en esta división.
        </Typography>
      );
    }

    const safeIndex = Math.min(index, slides.length - 1);
    const current = slides[safeIndex];
    const prev = safeIndex > 0 ? slides[safeIndex - 1] : null;
    const next = safeIndex < slides.length - 1 ? slides[safeIndex + 1] : null;

    return (
      <Box>
        {multiStage && <SectionHeading>{current.stageLabel}</SectionHeading>}
        <MatchFixtureList matches={current.matches} buildHref={buildMatchHref} />
        {slides.length > 1 && (
          <Stack
            direction="row"
            sx={{ justifyContent: 'space-between', alignItems: 'center', mt: 2 }}
          >
            <Button
              startIcon={<ArrowBackIcon />}
              disabled={!prev}
              onClick={() => setIndex(current => Math.max(0, current - 1))}
            >
              {prev ? formatRoundLabel(prev.matches[0]?.round ?? null) : ''}
            </Button>
            <Button
              endIcon={<ArrowForwardIcon />}
              disabled={!next}
              onClick={() =>
                setIndex(current => Math.min(slides.length - 1, current + 1))
              }
            >
              {next ? formatRoundLabel(next.matches[0]?.round ?? null) : ''}
            </Button>
          </Stack>
        )}
      </Box>
    );
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
      {sections.map(({ stage, label, matches: stageMatches }) => (
        <Box key={stage.id}>
          <SectionHeading>{label}</SectionHeading>
          <MatchFixtureList
            matches={stageMatches}
            exportTitle={label}
            buildHref={buildMatchHref}
          />
        </Box>
      ))}
    </Box>
  );
}
