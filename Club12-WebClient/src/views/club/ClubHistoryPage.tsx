import React, { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardContent,
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
import LoadingIndicator from '@/views/core/components/LoadingIndicator';
import TeamLogo from '@/views/core/components/TeamLogo';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

interface ClubSeasonRow {
  key: string;
  teamId: GUID;
  teamName: string;
  threeLetterCode: string;
  teamSlug: string;
  tournamentName: string;
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

  // One row per (team, season) pair, so the table reads as the club's
  // season-by-season trajectory. A team with no registered season still
  // shows a single row with a placeholder.
  const rows = useMemo<ClubSeasonRow[]>(() => {
    if (!club) {
      return [];
    }

    return club.teams.flatMap(team => {
      if (team.seasons.length === 0) {
        return [
          {
            key: `${team.teamId}-none`,
            teamId: team.teamId,
            teamName: team.name,
            threeLetterCode: team.threeLetterCode,
            teamSlug: team.slug,
            tournamentName: '—',
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
      }));
    });
  }, [club]);

  if (loading) {
    return <LoadingIndicator />;
  }

  if (!club) {
    return (
      <Card>
        <CardContent>
          <Typography variant="h6">Club no encontrado</Typography>
          <Typography
            variant="body2"
            sx={{ color: 'text.secondary', mt: 1 }}
          >
            No fue posible cargar el historial del club.
          </Typography>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardContent>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          sx={{
            alignItems: { xs: 'flex-start', sm: 'center' },
            justifyContent: 'space-between',
            mb: 2,
          }}
        >
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <TeamLogo
              teamName={club.name}
              logoUrl={club.logoUrl ?? ''}
              size={44}
            />
            <div>
              <Typography variant="h6">{club.name}</Typography>
              <Typography variant="body2" sx={{ color: 'text.secondary' }}>
                Historial del club
              </Typography>
            </div>
          </Stack>
          <Button
            variant="contained"
            color="primary"
            onClick={() => navigate(APP_ROUTES.panelTeams)}
          >
            Volver
          </Button>
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
      </CardContent>
    </Card>
  );
};

export default ClubHistoryPage;
