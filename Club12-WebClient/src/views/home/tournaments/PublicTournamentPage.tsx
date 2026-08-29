import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import {
  Box,
  Button,
  Divider,
  Grid,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton, TableSkeleton } from '@/views/core/components/skeletons';
import { GUID } from '@/modules/core/types/types';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useTeam } from '@/modules/team/hook/team.hook';
import { ITeamResponse } from '@/modules/team/type/team.d';
import { useDivision } from '@/modules/division/hook/division.hook';
import { divisionService } from '@/modules/division/service/division.service';
import { IDivisionResponse } from '@/modules/division/type/division';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import TeamLogo from '@/views/core/components/TeamLogo';
import PublicDivisionPanel from '@/views/home/tournaments/PublicDivisionPanel';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { TAB_CONTENT_MIN_HEIGHT } from '@/modules/core/constants/constants';
import { TOURNAMENT_STATUS_LABEL } from '@/modules/tournament/utils/tournamentDisplay';
import { formatDateAr } from '@/modules/core/utils/formatDate';

const formatDate = (value: Date | string) => formatDateAr(value);

const INFO_TAB = 'info';
const TEAMS_TAB = 'equipos';
const TAB_QUERY_PARAM = 'tab';

type Tab = typeof INFO_TAB | typeof TEAMS_TAB | GUID;

const ZONA_NAME_PATTERN = /^zona\s/i;

/**
 * Backend list order is arbitrary (insertion order), not display order.
 * "Zona X" divisions sort alphabetically first (A, B, C, D...), then any
 * other regular division (e.g. "Femenino"), then cross-division cups last.
 */
const orderDivisions = (divisions: IDivisionResponse[]): IDivisionResponse[] => {
  const zones = divisions.filter(d => !d.isCrossDivisionCup);
  const cups = divisions.filter(d => d.isCrossDivisionCup);

  const namedZones = zones
    .filter(d => ZONA_NAME_PATTERN.test(d.name))
    .sort((a, b) => a.name.localeCompare(b.name, 'es'));
  const otherZones = zones.filter(d => !ZONA_NAME_PATTERN.test(d.name));

  return [...namedZones, ...otherZones, ...cups];
};

export default function PublicTournamentPage() {
  const { tournamentId } = useParams<{ tournamentId: string }>();
  const navigate = useNavigate();
  const { tournament, getTournamentById } = useTournament();
  const { teams, getTeamsByFiltered } = useTeam();
  const { getDivisionsByFilters } = useDivision();
  const [loading, setLoading] = useState(false);
  const [divisions, setDivisions] = useState<IDivisionResponse[]>([]);
  const [divisionsLoading, setDivisionsLoading] = useState(false);

  /**
   * The active tab lives in the URL (not local state) so the browser back
   * button undoes one tab switch at a time instead of jumping straight out
   * of the page, and reloading/sharing a link lands on the same tab. Uses
   * `replace` so clicking through tabs doesn't pile up a history entry per
   * click — only the page navigation itself does.
   */
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get(TAB_QUERY_PARAM) ?? INFO_TAB) as Tab;
  const setTab = (value: Tab) => {
    setSearchParams(
      prev => {
        const next = new URLSearchParams(prev);
        next.set(TAB_QUERY_PARAM, value);
        return next;
      },
      { replace: true }
    );
  };

  const getTournamentRef = useRef(getTournamentById);
  const getTeamsRef = useRef(getTeamsByFiltered);
  const getDivisionsRef = useRef(getDivisionsByFilters);

  /**
   * The URL param can be either the tournament's real id or its slug, but
   * every downstream filter call (divisions/teams by tournament) needs the
   * real GUID foreign key, so those calls key off the resolved tournament
   * instead of the raw route param.
   */
  const tournamentGuid = tournament?.id;

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
    if (!tournamentGuid) return;
    let cancelled = false;

    const fetchDivisions = async () => {
      setDivisionsLoading(true);
      try {
        const response = await getDivisionsRef.current({
          tournamentId: tournamentGuid,
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
        if (!cancelled) setDivisions(orderDivisions(detailed));
      } finally {
        if (!cancelled) setDivisionsLoading(false);
      }
    };

    void fetchDivisions();
    return () => {
      cancelled = true;
    };
  }, [tournamentGuid]);

  useEffect(() => {
    if (!tournamentGuid || tab !== TEAMS_TAB) return;
    void getTeamsRef.current({ tournamentId: tournamentGuid, pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 });
  }, [tab, tournamentGuid]);

  const teamRows = useMemo(() => teams ?? [], [teams]);
  const activeDivision = useMemo(
    () => divisions.find(division => division.id === tab),
    [divisions, tab]
  );

  /**
   * A team has no direct division field — membership only exists through
   * its stage assignments, already surfaced in each division's `positions`.
   * A team can legitimately appear under more than one group (e.g. its
   * home zone AND a cross-division cup), so this groups rather than picks
   * a single owner. Anything not assigned to any division yet (newly
   * registered, not placed in a group) falls into "Sin división asignada".
   */
  const teamsByDivision = useMemo(() => {
    const groups = divisions
      .map(division => {
        const teamIds = new Set((division.positions ?? []).map(p => p.teamId));
        return { division, teams: teamRows.filter(team => teamIds.has(team.id)) };
      })
      .filter(group => group.teams.length > 0);

    const assignedTeamIds = new Set(groups.flatMap(g => g.teams.map(t => t.id)));
    const unassigned = teamRows.filter(team => !assignedTeamIds.has(team.id));

    return { groups, unassigned };
  }, [divisions, teamRows]);

  if (loading || (divisionsLoading && divisions.length === 0)) {
    return (
      <PageShell>
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (!tournament || (tournament.id !== tournamentId && tournament.slug !== tournamentId)) {
    return (
      <PageShell maxWidth="md">
        <Typography variant="h5" component="h1" sx={{
          mb: 2
        }}>Torneo no encontrado</Typography>
        <Typography sx={{ color: 'text.secondary', mb: 3 }}>
          El torneo que buscás no existe o ya no está disponible.
        </Typography>
        <Button onClick={() => navigate(APP_ROUTES.publicTournaments)}>Volver a torneos</Button>
      </PageShell>
    );
  }

  const status = tournament.status as TournamentStatus;

  const renderTeamGrid = (teamsToRender: ITeamResponse[]) => (
    <Grid container spacing={2}>
      {teamsToRender.map(team => (
        <Grid
          key={team.id}
          size={{
            xs: 12,
            sm: 6,
            md: 4
          }}>
          <Box
            onClick={() => navigate(APP_ROUTES.publicTeam.build(team.slug ?? team.id))}
            sx={{
              display: "flex",
              alignItems: "center",
              gap: 1.5,
              p: 1.5,
              border: '1px solid',
              borderColor: 'divider',
              borderRadius: 1,
              cursor: 'pointer',
              '&:hover': { bgcolor: 'action.hover' }
            }}>
            <TeamLogo teamName={team.name} logoUrl={team.logoUrl} size={36} />
            <Box>
              <Typography variant="body2" sx={{
                fontWeight: 500
              }}>{team.name}</Typography>
              <Typography variant="caption" sx={{
                color: "text.secondary"
              }}>{team.threeLetterCode}</Typography>
            </Box>
          </Box>
        </Grid>
      ))}
    </Grid>
  );

  return (
    <PageShell
      back={{
        label: 'Volver a torneos',
        onClick: () => navigate(APP_ROUTES.publicTournaments),
      }}
    >
      <Typography
        variant="h4"
        component="h1"
        sx={{
          fontWeight: "bold",
          mb: 0.5
        }}>
        {tournament.name}
      </Typography>
      <Typography
        variant="subtitle1"
        component="p"
        sx={{
          color: "text.secondary",
          mb: 3
        }}>
        {TOURNAMENT_STATUS_LABEL[status] ?? status}
      </Typography>

      <Divider sx={{ mb: 3 }} />

      <Tabs
        value={tab}
        onChange={(_, value: Tab) => setTab(value)}
        variant="scrollable"
        scrollButtons="auto"
        sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}
      >
        <Tab label="Información" value={INFO_TAB} />
        <Tab label="Equipos" value={TEAMS_TAB} />
        {divisions.map(division => (
          <Tab key={division.id} label={division.name} value={division.id} />
        ))}
      </Tabs>

      <Box sx={{ minHeight: TAB_CONTENT_MIN_HEIGHT }}>
      {tab === INFO_TAB && (
        <Grid container spacing={3}>
          <Grid size={12}>
            <Typography variant="subtitle2" component="p" sx={{
              color: "text.secondary"
            }}>Descripción</Typography>
            <Typography>{tournament.description || '—'}</Typography>
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 6
            }}>
            <Typography variant="subtitle2" component="p" sx={{
              color: "text.secondary"
            }}>Fecha de inicio</Typography>
            <Typography>{formatDate(tournament.startDate)}</Typography>
          </Grid>
          <Grid
            size={{
              xs: 12,
              sm: 6
            }}>
            <Typography variant="subtitle2" component="p" sx={{
              color: "text.secondary"
            }}>Cierre de inscripción</Typography>
            <Typography>{formatDate(tournament.teamRegistrationDeadline)}</Typography>
          </Grid>
        </Grid>
      )}

      {tab === TEAMS_TAB && (
        teamRows.length === 0 ? (
          <Typography sx={{
            color: "text.secondary"
          }}>No hay equipos inscriptos en este torneo.</Typography>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
            {teamsByDivision.groups.map(({ division, teams: divisionTeams }) => (
              <Box key={division.id}>
                <Typography variant="h6" component="h2" sx={{ mb: 2 }}>
                  {division.name}
                </Typography>
                {renderTeamGrid(divisionTeams)}
              </Box>
            ))}

            {teamsByDivision.unassigned.length > 0 && (
              <Box>
                <Typography variant="h6" component="h2" sx={{ mb: 2 }}>
                  Sin división asignada
                </Typography>
                {renderTeamGrid(teamsByDivision.unassigned)}
              </Box>
            )}
          </Box>
        )
      )}

      {tab !== INFO_TAB && tab !== TEAMS_TAB && (
        divisionsLoading && !activeDivision ? (
          <TableSkeleton rows={6} columns={4} />
        ) : activeDivision ? (
          // key forces a fresh mount per division — without it React reuses
          // the same instance across division switches and its lazy-loaded
          // state (stages/matches/brackets/top scorers) never resets, so a
          // different division's Partidos/Llaves/Goleadores view would keep
          // showing whichever division's data loaded first.
          <PublicDivisionPanel key={activeDivision.id} division={activeDivision} />
        ) : null
      )}
      </Box>
    </PageShell>
  );
}
