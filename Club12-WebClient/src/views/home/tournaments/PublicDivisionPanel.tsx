import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Box, Tab, Typography } from '@mui/material';
import { ListSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { TAB_CONTENT_MIN_HEIGHT } from '@/modules/core/constants/constants';
import { IDivisionResponse } from '@/modules/division/type/division';
import { buildCrossCupGroupQualificationRange } from '@/modules/division/utils/qualificationRange';
import { ITeamResponse } from '@/modules/team/type/team.d';
import PublicTeamGrid from '@/views/home/tournaments/PublicTeamGrid';
import SectionHeading from '@/views/core/components/SectionHeading';
import SecondaryTabs from '@/views/core/components/SecondaryTabs';
import DivisionScorersTable from '@/views/division/DivisionScorersTable';
import { MatchType } from '@/modules/core/enum/match/matchType';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { IStageResponse } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import DivisionStandings from '@/views/division/divisionStandings';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';
import Podium from '@/views/champion/Podium';
import { IPodium } from '@/modules/champion/type/champion.d';

const FETCH_PAGE_SIZE = 100;
const DEFAULT_SUB_TAB: DivisionSubTab = 'posiciones';
const VIEW_QUERY_PARAM = 'view';

type DivisionSubTab = 'equipos' | 'posiciones' | 'goleadores' | 'partidos' | 'playoff';

interface PublicDivisionPanelProps {
  division: IDivisionResponse;
  /** The teams that play in this division (shown in its "Equipos" tab). */
  teams: ITeamResponse[];
  /**
   * This division's podium (top three), when it has a decided champion. Shown
   * as a highlighted banner above the sub-tabs. Omitted/`null` while undecided.
   */
  podium?: IPodium | null;
}

const VALID_SUB_TABS: readonly DivisionSubTab[] = ['equipos', 'posiciones', 'goleadores', 'partidos', 'playoff'];
const isDivisionSubTab = (value: string | null): value is DivisionSubTab =>
  VALID_SUB_TABS.includes(value as DivisionSubTab);

/**
 * A single division's public content, scoped behind its own tournament-level
 * tab: standings, top scorers, fixtures and playoff bracket. Match/stage/
 * bracket and top-scorer data load lazily on first visit to their sub-tab
 * (not for every division up front), and loading state is always cleared in
 * a `finally` so a failed request can't leave the spinner running forever.
 */
export default function PublicDivisionPanel({ division, teams, podium }: PublicDivisionPanelProps) {
  /**
   * Lives in the URL for the same reason the parent tournament tab does:
   * the browser back button should undo one sub-tab switch at a time
   * instead of jumping out of the page entirely, and the exact view should
   * survive a refresh or a shared link.
   */
  const [searchParams, setSearchParams] = useSearchParams();
  const rawSubTab = searchParams.get(VIEW_QUERY_PARAM);
  const subTab = isDivisionSubTab(rawSubTab) ? rawSubTab : DEFAULT_SUB_TAB;
  const setSubTab = (value: DivisionSubTab) => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.set(VIEW_QUERY_PARAM, value);
        return next;
      },
      { replace: true }
    );
  };

  const crossCupGroupQualificationRange = useMemo(
    () => buildCrossCupGroupQualificationRange(division),
    [division]
  );

  const [stages, setStages] = useState<IStageResponse[]>([]);
  const [matches, setMatches] = useState<IMatchResponse[]>([]);
  const [seriesById, setSeriesById] = useState<Map<GUID, IMatchSeriesResponse>>(new Map());
  const [bracketGroups, setBracketGroups] = useState<BracketGroup[]>([]);
  const [structureLoading, setStructureLoading] = useState(false);
  const [structureLoaded, setStructureLoaded] = useState(false);

  useEffect(() => {
    if ((subTab !== 'partidos' && subTab !== 'playoff') || structureLoaded) return;
    let cancelled = false;

    const fetchStructure = async () => {
      setStructureLoading(true);
      try {
        // Regular-season and playoff matches are fetched separately, each
        // with their own page budget — a single division-wide fetch shares
        // FETCH_PAGE_SIZE between both, and a full round-robin's games can
        // easily outnumber the (usually much smaller) playoff bracket,
        // silently pushing it off the page: the bracket itself rendered
        // fine (it loads its scores from a separate per-stage MatchSeries
        // fetch below), but the "Partidos de playoff" list built from these
        // matches ended up empty.
        const [stagesResponse, regularMatchesResponse, playoffMatchesResponse] = await Promise.all([
          stageService.getStagesByFilters({ divisionId: division.id, pageSize: FETCH_PAGE_SIZE }),
          matchService.getMatchByFilter({ divisionId: division.id, type: MatchType.Regular, pageSize: FETCH_PAGE_SIZE }),
          matchService.getMatchByFilter({ divisionId: division.id, type: MatchType.Playoff, pageSize: FETCH_PAGE_SIZE }),
        ]);
        const stagesList = stagesResponse.data?.items ?? [];
        const matchesList = [
          ...(regularMatchesResponse.data?.items ?? []),
          ...(playoffMatchesResponse.data?.items ?? []),
        ];

        const eliminationStages = stagesList.filter(stage => stage.isElimination);
        const seriesStages = eliminationStages.filter(stage => stage.bestOf > 1);
        const seriesByStageId = new Map<GUID, IMatchSeriesResponse[]>();
        const nextSeriesById = new Map<GUID, IMatchSeriesResponse>();

        if (seriesStages.length > 0) {
          const seriesResponses = await Promise.all(
            seriesStages.map(stage =>
              matchSeriesService.getMatchSeriesByFilters({ stageId: stage.id, pageSize: FETCH_PAGE_SIZE })
            )
          );
          seriesStages.forEach((stage, index) => {
            const seriesList = seriesResponses[index].data?.items ?? [];
            seriesByStageId.set(stage.id, seriesList);
            seriesList.forEach(series => nextSeriesById.set(series.id, series));
          });
        }

        if (!cancelled) {
          setStages(stagesList);
          setMatches(matchesList);
          setSeriesById(nextSeriesById);
          setBracketGroups(buildBrackets(eliminationStages, matchesList, seriesByStageId));
        }
      } finally {
        if (!cancelled) {
          setStructureLoading(false);
          setStructureLoaded(true);
        }
      }
    };

    void fetchStructure();
    return () => {
      cancelled = true;
    };
  }, [subTab, structureLoaded, division.id]);

  // Elimination-stage matches live in the Playoff tab's bracket, not here —
  // otherwise a division with playoffs shows the same games twice. `stages`
  // is shared with the bracket fetch above (which needs every stage), so the
  // exclusion happens here rather than in the query.
  const groupStages = useMemo(
    () => stages.filter(stage => !stage.isElimination),
    [stages]
  );
  const matchSections = useMemo(
    () => buildDivisionFixtureSections(groupStages, matches, division.name),
    [groupStages, matches, division.name]
  );

  // The Playoff tab's counterpart to `matchSections`: a real, fecha-grouped
  // match list for the elimination stages, shown alongside the bracket
  // visual so a visitor can see every individual game of a best-of-N
  // series, not just the collapsed bracket card.
  const eliminationStages = useMemo(
    () => stages.filter(stage => stage.isElimination),
    [stages]
  );
  const playoffMatchSections = useMemo(
    () => buildDivisionFixtureSections(eliminationStages, matches, division.name),
    [eliminationStages, matches, division.name]
  );

  return (
    <Box>
      {podium?.first && (
        <Box
          sx={{
            mb: 3,
            p: { xs: 2, sm: 3 },
            borderRadius: 2,
            bgcolor: 'action.hover',
          }}
        >
          <SectionHeading>Campeones</SectionHeading>
          <Podium podium={podium} />
        </Box>
      )}

      <SecondaryTabs
        value={subTab}
        onChange={(_, value) => setSubTab(value as DivisionSubTab)}
        aria-label={`Vistas de ${division.name}`}
      >
        <Tab label="Equipos" value="equipos" />
        <Tab label="Posiciones" value="posiciones" />
        <Tab label="Goleadores" value="goleadores" />
        <Tab label="Partidos" value="partidos" />
        <Tab label="Playoff" value="playoff" />
      </SecondaryTabs>

      <Box sx={{ minHeight: TAB_CONTENT_MIN_HEIGHT }}>
      {subTab === 'equipos' && (
        <PublicTeamGrid
          teams={teams}
          emptyLabel="Todavía no hay equipos asignados a esta división."
        />
      )}

      {subTab === 'posiciones' &&
        (division.groupStandings && division.groupStandings.length > 1 ? (
          // Multi-group cross-division cup (HU-110): one standings table per
          // internal group, each labelled by its stage name ("Grupo 1", …) and
          // computed over that group's own matches. The division carries no
          // PlayoffMappings here (it pools every group into ONE bracket, not a
          // per-division cup breakdown — see DivisionProfile.cs), so the
          // qualifying rows are highlighted from `qualifiersPerGroup` directly:
          // the top N of EVERY group advance into the pooled knockout.
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {division.groupStandings.map(group => (
              <Box key={group.stageId}>
                <SectionHeading>{group.stageName}</SectionHeading>
                <DivisionStandings
                  positions={group.positions}
                  qualificationRanges={crossCupGroupQualificationRange}
                  showPrintSheet={false}
                />
              </Box>
            ))}
          </Box>
        ) : (
          <DivisionStandings
            positions={division.positions}
            divisionName={division.name}
            qualificationRanges={division.qualificationRanges}
          />
        ))}

      {subTab === 'goleadores' && (
        <DivisionScorersTable divisionId={division.id} divisionName={division.name} />
      )}

      {subTab === 'partidos' &&
        (structureLoading ? (
          <ListSkeleton items={6} />
        ) : matchSections.length === 0 ? (
          <Typography sx={{ color: 'text.secondary' }}>
            No hay partidos registrados en esta división.
          </Typography>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {matchSections.map(({ stage, label, matches: stageMatches }) => (
              <Box key={stage.id}>
                <SectionHeading>{label}</SectionHeading>
                <MatchFixtureList matches={stageMatches} exportTitle={label} />
              </Box>
            ))}
          </Box>
        ))}

      {subTab === 'playoff' &&
        (structureLoading ? (
          <ListSkeleton items={5} />
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            <PlayoffBrackets groups={bracketGroups} seriesById={seriesById} />

            {playoffMatchSections.length > 0 && (
              <Box>
                <SectionHeading>Partidos de playoff</SectionHeading>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                  {playoffMatchSections.map(({ stage, label, matches: stageMatches }) => (
                    <Box key={stage.id}>
                      <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 1 }}>
                        {label}
                      </Typography>
                      <MatchFixtureList matches={stageMatches} exportTitle={label} seriesById={seriesById} />
                    </Box>
                  ))}
                </Box>
              </Box>
            )}
          </Box>
        ))}
      </Box>
    </Box>
  );
}
