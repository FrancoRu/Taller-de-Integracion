import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { Box, Button, Grid, Stack, Tab, Tabs, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useStage } from '@/modules/stage/hook/stage.hook';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { IStageResponse } from '@/modules/stage/type/stage';
import { IMatchResponse } from '@/modules/match/type/match.d';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import { buildDivisionFixtureSections } from '@/modules/match/utils/divisionFixtureSections';
import DivisionStandings from '@/views/division/divisionStandings';
import DivisionScorersTable from '@/views/division/DivisionScorersTable';
import { buildCrossCupGroupQualificationRange } from '@/modules/division/utils/qualificationRange';
import DivisionFixture from '@/views/division/DivisionFixture';
import TeamLogo from '@/views/core/components/TeamLogo';
import PointDeductionManager from '@/views/division/PointDeductionManager';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';
import MatchFixtureList from '@/views/home/matches/MatchFixtureList';
import SectionHeading from '@/views/core/components/SectionHeading';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

/**
 * Explicit pageSize for the "Playoff" tab's Stage/Match fetch — the same
 * generous size PublicTournamentPage uses, so a deep elimination bracket
 * is never silently truncated by the default table page size.
 */
const BRACKET_FETCH_PAGE_SIZE = 100;

const DivisionPage: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: string }>();
  const navigate = useNavigate();
  const { division, getDivisionsById } = useDivision();
  const { tournament, getTournamentById } = useTournament();
  const { seedKnockoutStage } = useStage();
  const { role } = useAuth();
  const isAdminOrOwner =
    role === UserRolesType.Admin || role === UserRolesType.Owner;
  const [loading, setLoading] = useState(false);
  type DivisionTab =
    | 'detalle'
    | 'posiciones'
    | 'goleadores'
    | 'equipos'
    | 'partidos'
    | 'playoff';
  const DEFAULT_TAB: DivisionTab = 'detalle';
  const TAB_QUERY_PARAM = 'tab';
  // Kept in the URL (not local state) so leaving to a match's detail and
  // clicking "Volver" (which pops one history entry) lands back on the same
  // tab instead of resetting to Detalle — mirrors PublicTournamentPage's tab.
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get(TAB_QUERY_PARAM) ?? DEFAULT_TAB) as DivisionTab;
  const setTab = (value: DivisionTab) => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.set(TAB_QUERY_PARAM, value);
        return next;
      },
      { replace: true }
    );
  };
  const [bracketGroups, setBracketGroups] = useState<BracketGroup[]>([]);
  const [seriesById, setSeriesById] = useState<Map<GUID, IMatchSeriesResponse>>(new Map());
  const [bracketsLoading, setBracketsLoading] = useState(false);
  // Elimination-stage matches, kept alongside the bracket visual so the
  // Playoff tab can also show a real match list (like "Partidos", but
  // scoped to playoff stages) — a stage's individual games, not the
  // collapsed bracket card.
  const [playoffStages, setPlayoffStages] = useState<IStageResponse[]>([]);
  const [playoffMatches, setPlayoffMatches] = useState<IMatchResponse[]>([]);

  const targetDivisionId = useMemo(
    () => divisionId ?? division?.id,
    [division?.id, divisionId]
  );

  useEffect(() => {
    if (!targetDivisionId) {
      return;
    }

    const fetchDivision = async () => {
      setLoading(true);
      await getDivisionsById(targetDivisionId);
      setLoading(false);
    };

    void fetchDivision();
  }, [getDivisionsById, targetDivisionId]);

  useEffect(() => {
    if (!division?.tournamentId) {
      return;
    }

    if (tournament?.id === division.tournamentId) {
      return;
    }

    void getTournamentById(division.tournamentId);
  }, [division?.tournamentId, tournament?.id, getTournamentById]);

  const fetchBrackets = useCallback(async (resolvedDivisionId: GUID) => {
    setBracketsLoading(true);

    const [stagesResponse, matchesResponse] = await Promise.all([
      stageService.getStagesByFilters({
        divisionId: resolvedDivisionId,
        isElimination: true,
        pageSize: BRACKET_FETCH_PAGE_SIZE,
      }),
      matchService.getMatchByFilter({
        divisionId: resolvedDivisionId,
        pageSize: BRACKET_FETCH_PAGE_SIZE,
      }),
    ]);

    const stages = stagesResponse.data?.items ?? [];
    const matches = matchesResponse.data?.items ?? [];
    const seriesStages = stages.filter(stage => stage.bestOf > 1);

    const seriesByStageId = new Map<GUID, IMatchSeriesResponse[]>();
    const nextSeriesById = new Map<GUID, IMatchSeriesResponse>();

    if (seriesStages.length > 0) {
      const seriesResponses = await Promise.all(
        seriesStages.map(stage =>
          matchSeriesService.getMatchSeriesByFilters({
            stageId: stage.id,
            pageSize: BRACKET_FETCH_PAGE_SIZE,
          })
        )
      );

      seriesStages.forEach((stage, index) => {
        const seriesList = seriesResponses[index].data?.items ?? [];
        seriesByStageId.set(stage.id, seriesList);
        seriesList.forEach(series => nextSeriesById.set(series.id, series));
      });
    }

    setBracketGroups(buildBrackets(stages, matches, seriesByStageId));
    setSeriesById(nextSeriesById);
    setPlayoffStages(stages);
    setPlayoffMatches(matches);
    setBracketsLoading(false);
  }, []);

  useEffect(() => {
    // Filters below are keyed by the real division GUID, so the elimination
    // brackets only fetch once the slug-or-id param has resolved to a
    // loaded division.
    const resolvedDivisionId = division?.id;
    if (tab !== 'playoff' || !resolvedDivisionId) {
      return;
    }

    void fetchBrackets(resolvedDivisionId);
  }, [tab, division?.id, fetchBrackets]);

  const handleSeedBracket = useCallback(
    async (stageId: GUID) => {
      const resolvedDivisionId = division?.id;
      if (!resolvedDivisionId) {
        return;
      }

      const seeded = await seedKnockoutStage(stageId);
      if (seeded) {
        await fetchBrackets(resolvedDivisionId);
      }
    },
    [division?.id, seedKnockoutStage, fetchBrackets]
  );

  const handleMatchClick = useCallback(
    (matchId: GUID) => navigate(APP_ROUTES.panelMatch.build(matchId)),
    [navigate]
  );

  // Teams that can receive a point deduction: those present in the division's
  // standings (pooled positions cover both regular zones and multi-group cups),
  // deduped by team id.
  const divisionTeams = useMemo(() => {
    const pooled = [
      ...(division?.positions ?? []),
      ...(division?.groupStandings?.flatMap(group => group.positions) ?? []),
    ];
    const byId = new Map<
      GUID,
      { id: GUID; name: string; logoUrl?: string | null }
    >();
    pooled.forEach(position => {
      if (!byId.has(position.teamId)) {
        byId.set(position.teamId, {
          id: position.teamId,
          name: position.teamName,
          logoUrl: position.logoUrl,
        });
      }
    });
    return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name));
  }, [division?.positions, division?.groupStandings]);

  const crossCupGroupQualificationRange = useMemo(
    () => (division ? buildCrossCupGroupQualificationRange(division) : undefined),
    [division]
  );

  // A real, playoff-scoped match list — the same fecha-grouped rows the
  // "Partidos" tab shows, but for the elimination stages instead of the
  // regular-season ones — shown alongside the bracket visual so an admin
  // can see every individual game of a best-of-N series, not just the
  // collapsed bracket card.
  const playoffMatchSections = useMemo(
    () => buildDivisionFixtureSections(playoffStages, playoffMatches, division?.name ?? ''),
    [playoffStages, playoffMatches, division?.name]
  );

  if (!targetDivisionId) {
    return (
      <PageShell title="División">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No se recibió una división para visualizar.
        </Typography>
      </PageShell>
    );
  }

  if (loading) {
    return (
      <PageShell title="División">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (
    !division ||
    (division.id !== targetDivisionId && division.slug !== targetDivisionId)
  ) {
    return (
      <PageShell title="División no encontrada">
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar la información de la división.
        </Typography>
        <Button
          variant="text"
          onClick={() => navigate(APP_ROUTES.panelDivisions)}
          sx={{ mt: 2, px: 0 }}
        >
          Volver al listado
        </Button>
      </PageShell>
    );
  }

  return (
    <PageShell
      title={division.name}
      actions={
        <>
          <Button
            variant="contained"
            color="primary"
            onClick={() =>
              navigate(APP_ROUTES.panelDivisionEdit.build(division.slug ?? division.id))
            }
          >
            Editar
          </Button>
          <Button
            variant="outlined"
            onClick={() =>
              navigate(
                division.tournamentId
                  ? APP_ROUTES.panelTournamentDetail.build(
                      division.tournamentSlug ?? division.tournamentId
                    )
                  : APP_ROUTES.panelTournaments
              )
            }
          >
            Volver
          </Button>
        </>
      }
    >
      <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Equipos" value="equipos" />
          <Tab label="Posiciones" value="posiciones" />
          <Tab label="Goleadores" value="goleadores" />
          <Tab label="Partidos" value="partidos" />
          <Tab label="Playoff" value="playoff" />
        </Tabs>

        {tab === 'detalle' && (
          <Grid container spacing={2}>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Nombre
              </Typography>
              <Typography>{division.name}</Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Estado
              </Typography>
              <Typography>
                {division.isFinished ? 'Finalizada' : 'Activa'}
              </Typography>
            </Grid>
            <Grid
              size={{
                xs: 12,
                md: 6
              }}>
              <Typography variant="subtitle2" sx={{
                color: "text.secondary"
              }}>
                Equipos posicionados
              </Typography>
              <Typography>{division.positions?.length ?? 0}</Typography>
            </Grid>
          </Grid>
        )}

        {tab === 'equipos' && (
          divisionTeams.length === 0 ? (
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Esta división todavía no tiene equipos asignados.
            </Typography>
          ) : (
            <Grid container spacing={2}>
              {divisionTeams.map(team => (
                <Grid key={team.id} size={{ xs: 12, sm: 6, md: 4 }}>
                  <Box
                    onClick={() => navigate(APP_ROUTES.panelTeamDetail.build(team.id))}
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                      p: 1.5,
                      border: 1,
                      borderColor: 'divider',
                      borderRadius: 1,
                      cursor: 'pointer',
                      '&:hover': { borderColor: 'primary.main' },
                    }}
                  >
                    <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={36} />
                    <Typography sx={{ minWidth: 0 }} noWrap>
                      {team.name}
                    </Typography>
                  </Box>
                </Grid>
              ))}
            </Grid>
          )
        )}

        {tab === 'partidos' && (
          <DivisionFixture
            divisionId={division.id}
            divisionName={division.name}
            variant="carousel"
            buildMatchHref={m => APP_ROUTES.panelMatch.build(m.slug ?? m.id)}
          />
        )}

        {tab === 'posiciones' && isAdminOrOwner && (
          <Box sx={{ mb: 3 }}>
            <PointDeductionManager
              divisionId={division.id}
              teams={divisionTeams}
            />
          </Box>
        )}

        {tab === 'posiciones' &&
          (division.groupStandings && division.groupStandings.length > 1 ? (
            // Multi-group cross-division cup (HU-110): one standings table per
            // internal group, each labelled by its stage name ("Grupo 1", …).
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
              {division.groupStandings.map(group => (
                <Box key={group.stageId}>
                  <Typography variant="subtitle1" component="h3" sx={{ mb: 1.5 }}>
                    {group.stageName}
                  </Typography>
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

        {tab === 'goleadores' && (
          <DivisionScorersTable divisionId={division.id} divisionName={division.name} />
        )}

        {tab === 'playoff' &&
          (bracketsLoading ? (
            <DetailSkeleton />
          ) : (
            <Stack spacing={2}>
              {isAdminOrOwner && (
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  {bracketGroups
                    .filter(group => group.model.rounds.length > 0)
                    .map(group => {
                      const firstRound = group.model.rounds[0];
                      const alreadySeeded = firstRound.matches.some(
                        match => match.homeTeam || match.visitorTeam
                      );

                      if (alreadySeeded) {
                        return null;
                      }

                      return (
                        <Button
                          key={firstRound.stageId}
                          variant="outlined"
                          size="small"
                          onClick={() => void handleSeedBracket(firstRound.stageId)}
                        >
                          Sembrar bracket{group.bracketName ? ` — ${group.bracketName}` : ''}
                        </Button>
                      );
                    })}
                </Stack>
              )}
              <PlayoffBrackets
                groups={bracketGroups}
                seriesById={seriesById}
                onMatchClick={isAdminOrOwner ? handleMatchClick : undefined}
              />

              {playoffMatchSections.length > 0 && (
                <Box>
                  <SectionHeading>Partidos de playoff</SectionHeading>
                  <Stack spacing={3}>
                    {playoffMatchSections.map(({ stage, label, matches: stageMatches }) => (
                      <Box key={stage.id}>
                        <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 1 }}>
                          {label}
                        </Typography>
                        <MatchFixtureList
                          matches={stageMatches}
                          exportTitle={label}
                          buildHref={m => APP_ROUTES.panelMatch.build(m.slug ?? m.id)}
                        />
                      </Box>
                    ))}
                  </Stack>
                </Box>
              )}
            </Stack>
          ))}
    </PageShell>
  );
};

export default DivisionPage;
