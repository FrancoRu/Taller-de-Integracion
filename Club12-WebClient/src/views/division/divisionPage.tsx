import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Grid, Tab, Tabs, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import DivisionStandings from '@/views/division/divisionStandings';
import { buildCrossCupGroupQualificationRange } from '@/modules/division/utils/qualificationRange';
import DivisionFixture from '@/views/division/DivisionFixture';
import TeamLogo from '@/views/core/components/TeamLogo';
import PointDeductionManager from '@/views/division/PointDeductionManager';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';

/**
 * Explicit pageSize for the "Llaves" tab's Stage/Match fetch — the same
 * generous size PublicTournamentPage uses, so a deep elimination bracket
 * is never silently truncated by the default table page size.
 */
const BRACKET_FETCH_PAGE_SIZE = 100;

const DivisionPage: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: string }>();
  const navigate = useNavigate();
  const { division, getDivisionsById } = useDivision();
  const { tournament, getTournamentById } = useTournament();
  const { role } = useAuth();
  const isAdminOrOwner =
    role === UserRolesType.Admin || role === UserRolesType.Owner;
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<
    'detalle' | 'posiciones' | 'equipos' | 'partidos' | 'llaves'
  >('detalle');
  const [bracketGroups, setBracketGroups] = useState<BracketGroup[]>([]);
  const [seriesById, setSeriesById] = useState<Map<GUID, IMatchSeriesResponse>>(new Map());
  const [bracketsLoading, setBracketsLoading] = useState(false);

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

  useEffect(() => {
    // Filters below are keyed by the real division GUID, so the elimination
    // brackets only fetch once the slug-or-id param has resolved to a
    // loaded division.
    const resolvedDivisionId = division?.id;
    if (tab !== 'llaves' || !resolvedDivisionId) {
      return;
    }

    const fetchBrackets = async () => {
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
      setBracketsLoading(false);
    };

    void fetchBrackets();
  }, [tab, division?.id]);

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
          <Tab label="Partidos" value="partidos" />
          <Tab label="Llaves" value="llaves" />
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
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1.5,
                      p: 1.5,
                      border: 1,
                      borderColor: 'divider',
                      borderRadius: 1,
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
                  />
                </Box>
              ))}
            </Box>
          ) : (
            <DivisionStandings
              positions={division.positions}
              divisionId={division.id}
              divisionName={division.name}
              qualificationRanges={division.qualificationRanges}
            />
          ))}

        {tab === 'llaves' &&
          (bracketsLoading ? (
            <DetailSkeleton />
          ) : (
            <PlayoffBrackets groups={bracketGroups} seriesById={seriesById} />
          ))}
    </PageShell>
  );
};

export default DivisionPage;
