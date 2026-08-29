import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Button, Grid, Tab, Tabs, Typography } from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { stageService } from '@/modules/stage/service/stage.service';
import { matchService } from '@/modules/match/service/match.service';
import { matchSeriesService } from '@/modules/matchSeries/service/matchSeries.service';
import { IMatchSeriesResponse } from '@/modules/matchSeries/type/matchSeries.d';
import { buildBrackets } from '@/modules/playoff/buildBracket';
import { BracketGroup } from '@/modules/playoff/type/bracket.d';
import StagesPage from '@/views/stage/stagesPage';
import DivisionStandings from '@/views/division/divisionStandings';
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
  const [tab, setTab] = useState<'detalle' | 'posiciones' | 'fases' | 'llaves'>(
    'detalle'
  );
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

  const canGenerateStages = useMemo(() => {
    if (!division?.tournamentId || tournament?.id !== division.tournamentId) {
      return false;
    }

    const registrationDeadline = new Date(tournament.teamRegistrationDeadline);
    const registrationClosedByDate =
      !Number.isNaN(registrationDeadline.getTime()) &&
      registrationDeadline.getTime() <= Date.now();

    const registrationClosedByStatus =
      tournament.status === TournamentStatus.Ongoing ||
      tournament.status === TournamentStatus.Finished ||
      tournament.status === TournamentStatus.Canceled;

    return registrationClosedByDate || registrationClosedByStatus;
  }, [division?.tournamentId, tournament]);

  // Once the tournament has started its fixture is generated, so the division's
  // phase (fase) structure is frozen: adding or removing a stage would corrupt
  // the bracket. This mirrors the backend guard in StageService (the source of
  // truth) and is used to disable the "Nueva Fase" / delete affordances.
  const isTournamentStarted = useMemo(() => {
    if (!division?.tournamentId || tournament?.id !== division.tournamentId) {
      return false;
    }

    return (
      tournament.status === TournamentStatus.Ongoing ||
      tournament.status === TournamentStatus.Finished
    );
  }, [division?.tournamentId, tournament]);

  // Teams that can receive a point deduction: those present in the division's
  // standings (pooled positions cover both regular zones and multi-group cups),
  // deduped by team id.
  const divisionTeams = useMemo(() => {
    const pooled = [
      ...(division?.positions ?? []),
      ...(division?.groupStandings?.flatMap(group => group.positions) ?? []),
    ];
    const byId = new Map<GUID, { id: GUID; name: string }>();
    pooled.forEach(position => {
      if (!byId.has(position.teamId)) {
        byId.set(position.teamId, {
          id: position.teamId,
          name: position.teamName,
        });
      }
    });
    return [...byId.values()].sort((a, b) => a.name.localeCompare(b.name));
  }, [division?.positions, division?.groupStandings]);

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
        <Button
          variant="contained"
          color="primary"
          onClick={() => navigate(APP_ROUTES.panelDivisions)}
        >
          Volver
        </Button>
      }
    >
      <Tabs
          value={tab}
          onChange={(_, value) => setTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab label="Detalle" value="detalle" />
          <Tab label="Posiciones" value="posiciones" />
          <Tab label="Fases" value="fases" />
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
                  <DivisionStandings positions={group.positions} />
                </Box>
              ))}
            </Box>
          ) : (
            <DivisionStandings
              positions={division.positions}
              divisionId={division.id}
              divisionName={division.name}
            />
          ))}

        {tab === 'fases' && (
          <StagesPage
            divisionId={division.id}
            showGenerateStagesButton={canGenerateStages}
            stageStructureLocked={isTournamentStarted}
            title={undefined}
            wrapInCard={false}
          />
        )}

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
