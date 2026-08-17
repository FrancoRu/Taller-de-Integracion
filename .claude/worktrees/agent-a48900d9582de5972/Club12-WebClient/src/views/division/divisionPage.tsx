import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Box, Card, CardContent, CircularProgress, Grid, Tab, Tabs, Typography } from '@mui/material';
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
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import PlayoffBrackets from '@/views/playoff/PlayoffBrackets';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

/**
 * Explicit pageSize for the "Llaves" tab's Stage/Match fetch — the same
 * generous size PublicTournamentPage uses, so a deep elimination bracket
 * is never silently truncated by the default table page size.
 */
const BRACKET_FETCH_PAGE_SIZE = 100;

const DivisionPage: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: GUID }>();
  const navigate = useNavigate();
  const { division, getDivisionsById } = useDivision();
  const { tournament, getTournamentById } = useTournament();
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
    if (tab !== 'llaves' || !targetDivisionId) {
      return;
    }

    const fetchBrackets = async () => {
      setBracketsLoading(true);

      const [stagesResponse, matchesResponse] = await Promise.all([
        stageService.getStagesByFilters({
          divisionId: targetDivisionId,
          isElimination: true,
          pageSize: BRACKET_FETCH_PAGE_SIZE,
        }),
        matchService.getMatchByFilter({
          divisionId: targetDivisionId,
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
  }, [tab, targetDivisionId]);

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

  if (!targetDivisionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">División</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No se recibió una división para visualizar.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!division || division.id !== targetDivisionId) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">División no encontrada</Typography>
          <Typography
            variant="body2"
            sx={{
              color: "text.secondary",
              mt: 1
            }}>
            No fue posible cargar la información de la división.
          </Typography>
          <Typography
            component="button"
            onClick={() => navigate(APP_ROUTES.panelDivisions)}
            sx={{
              mt: 2,
              border: 0,
              background: 'none',
              color: 'primary.main',
              cursor: 'pointer',
              p: 0,
            }}
          >
            Volver al listado
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Typography variant="h6" sx={{
          mb: 2
        }}>
          {division.name}
        </Typography>

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

        {tab === 'posiciones' && (
          <DivisionStandings
            positions={division.positions}
            divisionId={targetDivisionId}
            divisionName={division.name}
          />
        )}

        {tab === 'fases' && (
          <StagesPage
            divisionId={targetDivisionId}
            showGenerateStagesButton={canGenerateStages}
            title={undefined}
            wrapInCard={false}
          />
        )}

        {tab === 'llaves' &&
          (bracketsLoading ? (
            <Box
              sx={{
                display: "flex",
                justifyContent: "center",
                py: 5
              }}>
              <CircularProgress />
            </Box>
          ) : (
            <PlayoffBrackets groups={bracketGroups} seriesById={seriesById} />
          ))}
      </CardContent>
    </Card>
  );
};

export default DivisionPage;
