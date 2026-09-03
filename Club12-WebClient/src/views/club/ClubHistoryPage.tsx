import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { useClub } from '@/modules/club/hook/club.hook';
import { GUID } from '@/modules/core/types/types';
import PageShell from '@/views/core/components/PageShell';
import { TableSkeleton } from '@/views/core/components/skeletons';
import TeamLogo from '@/views/core/components/TeamLogo';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

interface ClubSeasonRow {
  key: string;
  teamId: GUID;
  teamName: string;
  threeLetterCode: string;
  teamSlug: string;
  tournamentName: string;
  /** ISO start date of the season; '' for a team with no registered season. */
  startDate: string;
}

/**
 * Club history / trajectory view (HU-99). Shows the club header and, per
 * season, the team and the tournaments it was registered in.
 */
const ClubHistoryPage: React.FC = () => {
  const { idOrSlug } = useParams<{ idOrSlug: string }>();
  const navigate = useNavigate();
  const { club, getClubHistory } = useClub();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!idOrSlug) {
      return;
    }

    const fetchHistory = async () => {
      setLoading(true);
      await getClubHistory(idOrSlug);
      setLoading(false);
    };

    void fetchHistory();
  }, [getClubHistory, idOrSlug]);

  // Canonicalise the URL to the club slug. The team page's "Ver historial del
  // club" button navigates here with the club GUID (all it carries), so once
  // the real slug is known, replace the history entry with the slug URL.
  useEffect(() => {
    if (club && idOrSlug && club.slug && idOrSlug !== club.slug) {
      navigate(APP_ROUTES.panelClub.build(club.slug), { replace: true });
    }
  }, [club, idOrSlug, navigate]);

  // One row per (team, season) pair, so the table reads as the club's
  // season-by-season trajectory. A team with no registered season still
  // shows a single row with a placeholder. Rows across every team are then
  // sorted newest-season-first; placeholder rows (no start date) sort last.
  const rows = useMemo<ClubSeasonRow[]>(() => {
    if (!club) {
      return [];
    }

    const unsorted = club.teams.flatMap(team => {
      if (team.seasons.length === 0) {
        return [
          {
            key: `${team.teamId}-none`,
            teamId: team.teamId,
            teamName: team.name,
            threeLetterCode: team.threeLetterCode,
            teamSlug: team.slug,
            tournamentName: '—',
            startDate: '',
          },
        ];
      }

      return team.seasons.map(season => ({
        key: `${team.teamId}-${season.tournamentId}`,
        teamId: team.teamId,
        teamName: team.name,
        threeLetterCode: team.threeLetterCode,
        teamSlug: team.slug,
        tournamentName: season.tournamentName ?? '—',
        startDate: season.startDate ?? '',
      }));
    });

    return unsorted.sort((a, b) => b.startDate.localeCompare(a.startDate));
  }, [club]);

  if (loading) {
    return (
      <PageShell title="Historial del club">
        <TableSkeleton columns={3} />
      </PageShell>
    );
  }

  if (!club) {
    return (
      <PageShell
        title="Club no encontrado"
        back={{ label: 'Volver', onClick: () => navigate(-1) }}
      >
        <Typography variant="body2" sx={{ color: 'text.secondary' }}>
          No fue posible cargar el historial del club.
        </Typography>
      </PageShell>
    );
  }

  return (
    <PageShell
      back={{ label: 'Volver', onClick: () => navigate(-1) }}
    >
        <Stack
          direction="row"
          spacing={1.5}
          sx={{ alignItems: 'center', mb: 3 }}
        >
          <TeamLogo
            teamName={club.name}
            logoUrl={club.logoUrl ?? ''}
            size={44}
          />
          <div>
            <Typography variant="h4" component="h1">
              {club.name}
            </Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary' }}>
              Historial del club
            </Typography>
          </div>
        </Stack>

        {rows.length > 0 ? (
          <TableContainer>
            <Table size="small" aria-label="Historial por temporada">
              <TableHead>
                <TableRow>
                  <TableCell>Equipo</TableCell>
                  <TableCell>Código</TableCell>
                  <TableCell>Temporada</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {rows.map(row => (
                  <TableRow key={row.key} hover>
                    <TableCell>
                      <Typography
                        component="button"
                        onClick={() =>
                          navigate(
                            APP_ROUTES.panelTeamDetail.build(row.teamSlug)
                          )
                        }
                        sx={{
                          border: 0,
                          background: 'none',
                          color: 'primary.main',
                          cursor: 'pointer',
                          p: 0,
                          font: 'inherit',
                          textAlign: 'left',
                        }}
                      >
                        {row.teamName}
                      </Typography>
                    </TableCell>
                    <TableCell>{row.threeLetterCode}</TableCell>
                    <TableCell>{row.tournamentName}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Typography variant="body2" sx={{ color: 'text.secondary' }}>
            Este club todavía no tiene temporadas registradas.
          </Typography>
        )}
    </PageShell>
  );
};

export default ClubHistoryPage;
