import { useEffect, useMemo, useState } from 'react';
import {
  Box,
  CircularProgress,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { IDivisionResponse } from '@/modules/division/type/division';
import { IScorerByPlayerResponse } from '@/modules/scorer/type/scorer.d';
import { scorerService } from '@/modules/scorer/service/scorer.service';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { IStageResponse } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import { stageLabel } from '@/modules/stage/utils/stageLabel';
import DivisionStandings from '@/views/division/divisionStandings';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';

const FETCH_PAGE_SIZE = 100;

type DivisionSubTab = 'posiciones' | 'goleadores' | 'partidos' | 'llaves';

interface PublicDivisionPanelProps {
  division: IDivisionResponse;
}

/**
 * A single division's public content, scoped behind its own tournament-level
 * tab: standings, top scorers, fixtures and playoff bracket. Match/stage/
 * bracket and top-scorer data load lazily on first visit to their sub-tab
 * (not for every division up front), and loading state is always cleared in
 * a `finally` so a failed request can't leave the spinner running forever.
 */
export default function PublicDivisionPanel({ division }: PublicDivisionPanelProps) {
  const [subTab, setSubTab] = useState<DivisionSubTab>('posiciones');

  const [topScores, setTopScores] = useState<IScorerByPlayerResponse[]>([]);
  const [topScoresLoading, setTopScoresLoading] = useState(false);
  const [topScoresLoaded, setTopScoresLoaded] = useState(false);

  const [stages, setStages] = useState<IStageResponse[]>([]);
  const [matches, setMatches] = useState<IMatchResponse[]>([]);
  const [seriesById, setSeriesById] = useState<Map<GUID, IMatchSeriesResponse>>(new Map());
  const [bracketGroups, setBracketGroups] = useState<BracketGroup[]>([]);
  const [structureLoading, setStructureLoading] = useState(false);
  const [structureLoaded, setStructureLoaded] = useState(false);

  useEffect(() => {
    if (subTab !== 'goleadores' || topScoresLoaded) return;
    let cancelled = false;

    const fetchTopScores = async () => {
      setTopScoresLoading(true);
      try {
        const response = await scorerService.getScorersByPlayerFiltered({
          divisionId: division.id,
          pageSize: FETCH_PAGE_SIZE,
          pageNumber: 1,
        });
        if (!cancelled) setTopScores(response.data?.items ?? []);
      } finally {
        if (!cancelled) {
          setTopScoresLoading(false);
          setTopScoresLoaded(true);
        }
      }
    };

    void fetchTopScores();
    return () => {
      cancelled = true;
    };
  }, [subTab, topScoresLoaded, division.id]);

  useEffect(() => {
    if ((subTab !== 'partidos' && subTab !== 'llaves') || structureLoaded) return;
    let cancelled = false;

    const fetchStructure = async () => {
      setStructureLoading(true);
      try {
        const [stagesResponse, matchesResponse] = await Promise.all([
          stageService.getStagesByFilters({ divisionId: division.id, pageSize: FETCH_PAGE_SIZE }),
          matchService.getMatchByFilter({ divisionId: division.id, pageSize: FETCH_PAGE_SIZE }),
        ]);
        const stagesList = stagesResponse.data?.items ?? [];
        const matchesList = matchesResponse.data?.items ?? [];

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

  const matchSections = useMemo(() => {
    const stageIdsInOrder = [...stages].sort((a, b) => a.order - b.order);
    return stageIdsInOrder
      .map(stage => ({ stage, matches: matches.filter(match => match.stageId === stage.id) }))
      .filter(section => section.matches.length > 0);
  }, [stages, matches]);

  return (
    <Box>
      <Tabs
        value={subTab}
        onChange={(_, value: DivisionSubTab) => setSubTab(value)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Posiciones" value="posiciones" />
        <Tab label="Goleadores" value="goleadores" />
        <Tab label="Partidos" value="partidos" />
        <Tab label="Llaves" value="llaves" />
      </Tabs>

      {subTab === 'posiciones' && (
        <DivisionStandings
          positions={division.positions}
          divisionId={division.id}
          divisionName={division.name}
        />
      )}

      {subTab === 'goleadores' &&
        (topScoresLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
            <CircularProgress />
          </Box>
        ) : topScores.length === 0 ? (
          <Typography sx={{ color: 'text.secondary' }}>
            No hay goleadores registrados para esta división.
          </Typography>
        ) : (
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>#</TableCell>
                  <TableCell>Jugador</TableCell>
                  <TableCell align="center">Puntos</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {topScores.map((row, index) => (
                  <TableRow key={row.playerId} hover>
                    <TableCell>{index + 1}</TableCell>
                    <TableCell>{row.fullName}</TableCell>
                    <TableCell align="center">{row.points}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ))}

      {subTab === 'partidos' &&
        (structureLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
            <CircularProgress />
          </Box>
        ) : matchSections.length === 0 ? (
          <Typography sx={{ color: 'text.secondary' }}>
            No hay partidos registrados en esta división.
          </Typography>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {matchSections.map(({ stage, matches: stageMatches }) => (
              <Box key={stage.id}>
                <Typography variant="subtitle1" component="h3" sx={{ mb: 1.5 }}>
                  {stageLabel(stage)}
                </Typography>
                <MatchFixtureList matches={stageMatches} />
              </Box>
            ))}
          </Box>
        ))}

      {subTab === 'llaves' &&
        (structureLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
            <CircularProgress />
          </Box>
        ) : (
          <PlayoffBrackets groups={bracketGroups} seriesById={seriesById} />
        ))}
    </Box>
  );
}
