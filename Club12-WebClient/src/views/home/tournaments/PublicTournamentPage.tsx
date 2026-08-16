import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Grid,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { useDivision } from '@/modules/division/hook/division.hook';
import { divisionService } from '@/modules/division/service/division.service';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IDivisionResponse } from '@/modules/division/type/division';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { IStageResponse } from '@/modules/stage/type/stage.d';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { stageLabel } from '@/modules/stage/utils/stageLabel';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import TeamLogo from '@/views/core/components/TeamLogo';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import DivisionStandings from '@/views/division/divisionStandings';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';

const STRUCTURAL_FETCH_PAGE_SIZE = 100;

const STATUS_LABEL: Record<TournamentStatus, string> = {
  Scheduled: 'Programado',
  OpenForRegistration: 'Inscripción abierta',
  Ongoing: 'En curso',
  Finished: 'Finalizado',
  Canceled: 'Cancelado',
};

const formatDate = (value: Date | string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString('es-AR');
};

type Tab = 'info' | 'posiciones' | 'equipos' | 'partidos' | 'llaves';

interface DivisionSections {
  zones: IDivisionResponse[];
  cups: IDivisionResponse[];
}

const splitDivisions = (divisions: IDivisionResponse[]): DivisionSections => ({
  zones: divisions.filter(d => !d.isCrossDivisionCup),
  cups: divisions.filter(d => d.isCrossDivisionCup),
});

export default function PublicTournamentPage() {
  const { tournamentId } = useParams<{ tournamentId: GUID }>();
  const navigate = useNavigate();
  const { tournament, getTournamentById } = useTournament();
  const { teams, getTeamsByFiltered } = useTeam();
  const { getDivisionsByFilters } = useDivision();
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<Tab>('info');
  const [standings, setStandings] = useState<IDivisionResponse[]>([]);
  const [standingsLoading, setStandingsLoading] = useState(false);
  const [structuralDivisions, setStructuralDivisions] = useState<IDivisionResponse[]>([]);
  const [stagesByDivision, setStagesByDivision] = useState<Record<GUID, IStageResponse[]>>({});
  const [matchesByDivision, setMatchesByDivision] = useState<Record<GUID, IMatchResponse[]>>({});
  const [bracketsByDivision, setBracketsByDivision] = useState<Record<GUID, BracketGroup[]>>({});
  const [seriesById, setSeriesById] = useState<Map<GUID, IMatchSeriesResponse>>(new Map());
  const [structuralLoading, setStructuralLoading] = useState(false);
  const [structuralLoaded, setStructuralLoaded] = useState(false);

  const getTournamentRef = useRef(getTournamentById);
  const getTeamsRef = useRef(getTeamsByFiltered);
  const getDivisionsRef = useRef(getDivisionsByFilters);

  useEffect(() => { getTournamentRef.current = getTournamentById; }, [getTournamentById]);
  useEffect(() => { getTeamsRef.current = getTeamsByFiltered; }, [getTeamsByFiltered]);
  useEffect(() => { getDivisionsRef.current = getDivisionsByFilters; }, [getDivisionsByFilters]);

  useEffect(() => {
    if (!tournamentId) return;
    const fetch = async () => {
      setLoading(true);
      await getTournamentRef.current(tournamentId);
      setLoading(false);
    };
    void fetch();
  }, [tournamentId]);

  useEffect(() => {
    if (!tournamentId || tab !== 'posiciones') return;
    const fetchStandings = async () => {
      setStandingsLoading(true);
      const response = await getDivisionsRef.current({
        tournamentId,
        pageSize: PUBLIC_LISTING_PAGE_SIZE,
        pageNumber: 1,
      });
      const divisionsList = response?.items ?? [];
      const detailed = await Promise.all(
        divisionsList.map(async division => {
          const detail = await divisionService.getDivisionsById(division.id);
          return detail?.data ?? division;
        })
      );
      setStandings(detailed);
      setStandingsLoading(false);
    };
    void fetchStandings();
  }, [tab, tournamentId]);

  useEffect(() => {
    if (!tournamentId || tab !== 'equipos') return;
    void getTeamsRef.current({ tournamentId, pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 });
  }, [tab, tournamentId]);

  useEffect(() => {
    if (!tournamentId || structuralLoaded || (tab !== 'partidos' && tab !== 'llaves')) return;

    const fetchStructure = async () => {
      setStructuralLoading(true);

      const divisionsResponse = await getDivisionsRef.current({
        tournamentId,
        pageSize: PUBLIC_LISTING_PAGE_SIZE,
        pageNumber: 1,
      });
      const divisionsList = divisionsResponse?.items ?? [];
      setStructuralDivisions(divisionsList);

      const nextSeriesById = new Map<GUID, IMatchSeriesResponse>();
      const nextStagesByDivision: Record<GUID, IStageResponse[]> = {};
      const nextMatchesByDivision: Record<GUID, IMatchResponse[]> = {};
      const nextBracketsByDivision: Record<GUID, BracketGroup[]> = {};

      await Promise.all(
        divisionsList.map(async division => {
          const [stagesResponse, matchesResponse] = await Promise.all([
            stageService.getStagesByFilters({
              divisionId: division.id,
              pageSize: STRUCTURAL_FETCH_PAGE_SIZE,
            }),
            matchService.getMatchByFilter({
              divisionId: division.id,
              pageSize: STRUCTURAL_FETCH_PAGE_SIZE,
            }),
          ]);

          const stages = stagesResponse.data?.items ?? [];
          const matches = matchesResponse.data?.items ?? [];
          nextStagesByDivision[division.id] = stages;
          nextMatchesByDivision[division.id] = matches;

          const eliminationStages = stages.filter(stage => stage.isElimination);
          const seriesStages = eliminationStages.filter(stage => stage.bestOf > 1);

          const seriesByStageId = new Map<GUID, IMatchSeriesResponse[]>();
          if (seriesStages.length > 0) {
            const seriesResponses = await Promise.all(
              seriesStages.map(stage =>
                matchSeriesService.getMatchSeriesByFilters({
                  stageId: stage.id,
                  pageSize: STRUCTURAL_FETCH_PAGE_SIZE,
                })
              )
            );

            seriesStages.forEach((stage, index) => {
              const seriesList = seriesResponses[index].data?.items ?? [];
              seriesByStageId.set(stage.id, seriesList);
              seriesList.forEach(series => nextSeriesById.set(series.id, series));
            });
          }

          nextBracketsByDivision[division.id] = buildBrackets(eliminationStages, matches, seriesByStageId);
        })
      );

      setStagesByDivision(nextStagesByDivision);
      setMatchesByDivision(nextMatchesByDivision);
      setBracketsByDivision(nextBracketsByDivision);
      setSeriesById(nextSeriesById);
      setStructuralLoading(false);
      setStructuralLoaded(true);
    };

    void fetchStructure();
  }, [tab, tournamentId, structuralLoaded]);

  const teamRows = useMemo(() => teams ?? [], [teams]);
  const standingsSections = useMemo(() => splitDivisions(standings), [standings]);
  const structuralSections = useMemo(() => splitDivisions(structuralDivisions), [structuralDivisions]);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" py={10}>
        <CircularProgress />
      </Box>
    );
  }

  if (!tournament || tournament.id !== tournamentId) {
    return (
      <Container maxWidth="md" sx={{ py: 5 }}>
        <Typography variant="h5" component="h1" mb={2}>Torneo no encontrado</Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicTournaments)}>Volver a torneos</Button>
      </Container>
    );
  }

  const status = tournament.status as TournamentStatus;

  const renderDivisionMatches = (division: IDivisionResponse) => {
    const stages = stagesByDivision[division.id] ?? [];
    const matches = matchesByDivision[division.id] ?? [];
    const stageIdsInOrder = [...stages].sort((a, b) => a.order - b.order);

    const sections = stageIdsInOrder
      .map(stage => ({
        stage,
        matches: matches.filter(match => match.stageId === stage.id),
      }))
      .filter(section => section.matches.length > 0);

    if (sections.length === 0) {
      return (
        <Typography color="text.secondary" mb={2}>
          No hay partidos registrados en esta división.
        </Typography>
      );
    }

    return (
      <Box display="flex" flexDirection="column" gap={3}>
        {sections.map(({ stage, matches: stageMatches }) => (
          <Box key={stage.id}>
            <Typography variant="subtitle1" component="h3" mb={1.5}>
              {stageLabel(stage)}
            </Typography>
            <MatchFixtureList matches={stageMatches} />
          </Box>
        ))}
      </Box>
    );
  };

  const renderDivisionSections = (
    sections: DivisionSections,
    renderDivision: (division: IDivisionResponse) => React.ReactNode
  ) => (
    <Box display="flex" flexDirection="column" gap={5}>
      {sections.zones.length > 0 && (
        <Box display="flex" flexDirection="column" gap={4}>
          <Typography variant="overline" color="text.secondary">
            Divisiones
          </Typography>
          {sections.zones.map(division => (
            <Box key={division.id}>
              <Typography variant="h6" component="h2" mb={2}>
                {division.name}
              </Typography>
              {renderDivision(division)}
            </Box>
          ))}
        </Box>
      )}

      {sections.cups.length > 0 && (
        <Box display="flex" flexDirection="column" gap={4}>
          <Typography variant="overline" color="text.secondary">
            Copa
          </Typography>
          {sections.cups.map(division => (
            <Box key={division.id}>
              <Typography variant="h6" component="h2" mb={2}>
                {division.name}
              </Typography>
              {renderDivision(division)}
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Button onClick={() => navigate(APP_ROUTES.publicTournaments)} sx={{ mb: 3, pl: 0 }} color="inherit">
        ← Volver a torneos
      </Button>

      <Typography variant="h4" component="h1" fontWeight="bold" mb={0.5}>
        {tournament.name}
      </Typography>
      <Typography variant="subtitle1" component="p" color="text.secondary" mb={3}>
        {STATUS_LABEL[status] ?? status}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Tabs
        value={tab}
        onChange={(_, value: Tab) => setTab(value)}
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Información" value="info" />
        <Tab label="Posiciones" value="posiciones" />
        <Tab label="Equipos" value="equipos" />
        <Tab label="Partidos" value="partidos" />
        <Tab label="Llaves" value="llaves" />
      </Tabs>

      {tab === 'info' && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Typography variant="subtitle2" component="p" color="text.secondary">Descripción</Typography>
            <Typography>{tournament.description || '—'}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" component="p" color="text.secondary">Fecha de inicio</Typography>
            <Typography>{formatDate(tournament.startDate)}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" component="p" color="text.secondary">Cierre de inscripción</Typography>
            <Typography>{formatDate(tournament.teamRegistrationDeadline)}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" component="p" color="text.secondary">Equipos mínimos</Typography>
            <Typography>{tournament.minTeams}</Typography>
          </Grid>
          <Grid item xs={12} sm={6}>
            <Typography variant="subtitle2" component="p" color="text.secondary">Equipos máximos</Typography>
            <Typography>{tournament.maxTeams}</Typography>
          </Grid>
        </Grid>
      )}

      {tab === 'posiciones' &&
        (standingsLoading ? (
          <Box display="flex" justifyContent="center" py={5}>
            <CircularProgress />
          </Box>
        ) : standings.length === 0 ? (
          <Typography color="text.secondary">
            No hay posiciones disponibles para este torneo.
          </Typography>
        ) : (
          renderDivisionSections(standingsSections, division => (
            <DivisionStandings
              positions={division.positions}
              divisionId={division.id}
              divisionName={division.name}
            />
          ))
        ))}

      {tab === 'equipos' && (
        teamRows.length === 0 ? (
          <Typography color="text.secondary">No hay equipos inscriptos en este torneo.</Typography>
        ) : (
          <Grid container spacing={2}>
            {teamRows.map(team => (
              <Grid item xs={12} sm={6} md={4} key={team.id}>
                <Box
                  display="flex"
                  alignItems="center"
                  gap={1.5}
                  sx={{
                    p: 1.5,
                    border: '1px solid',
                    borderColor: 'divider',
                    borderRadius: 1,
                    cursor: 'pointer',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                  onClick={() => navigate(APP_ROUTES.publicTeam.build(team.id))}
                >
                  <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={36} />
                  <Box>
                    <Typography variant="body2" fontWeight={500}>{team.name}</Typography>
                    <Typography variant="caption" color="text.secondary">{team.threeLetterCode}</Typography>
                  </Box>
                </Box>
              </Grid>
            ))}
          </Grid>
        )
      )}

      {tab === 'partidos' &&
        (structuralLoading ? (
          <Box display="flex" justifyContent="center" py={5}>
            <CircularProgress />
          </Box>
        ) : structuralDivisions.length === 0 ? (
          <Typography color="text.secondary">
            No hay divisiones disponibles para este torneo.
          </Typography>
        ) : (
          renderDivisionSections(structuralSections, renderDivisionMatches)
        ))}

      {tab === 'llaves' &&
        (structuralLoading ? (
          <Box display="flex" justifyContent="center" py={5}>
            <CircularProgress />
          </Box>
        ) : structuralDivisions.length === 0 ? (
          <Typography color="text.secondary">
            No hay divisiones disponibles para este torneo.
          </Typography>
        ) : (
          renderDivisionSections(structuralSections, division => (
            <PlayoffBrackets
              groups={bracketsByDivision[division.id] ?? []}
              seriesById={seriesById}
            />
          ))
        ))}
    </Container>
  );
}
