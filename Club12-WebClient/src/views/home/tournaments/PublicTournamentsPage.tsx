import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Grid,
  Typography,
} from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { ITournamentResponse } from '@/modules/tournament/type/tournament';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import {
  TOURNAMENT_STATUS_LABEL,
  TOURNAMENT_STATUS_COLOR,
  formatTournamentDate,
} from '@/modules/tournament/utils/tournamentDisplay';

export function TournamentCard({ tournament }: { tournament: ITournamentResponse }) {
  const navigate = useNavigate();
  const status = tournament.status as TournamentStatus;

  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea
        onClick={() => navigate(APP_ROUTES.publicTournament.build(tournament.id))}
        sx={{ height: '100%', alignItems: 'flex-start', display: 'flex', flexDirection: 'column' }}
      >
        <CardContent sx={{ width: '100%' }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "flex-start",
              mb: 1
            }}>
            <Typography
              variant="h6"
              component="h2"
              sx={{
                lineHeight: 1.3,
                flex: 1,
                mr: 1
              }}>
              {tournament.name}
            </Typography>
            <Chip
              label={TOURNAMENT_STATUS_LABEL[status] ?? status}
              color={TOURNAMENT_STATUS_COLOR[status] ?? 'default'}
              size="small"
              variant={status === 'Scheduled' ? 'outlined' : 'filled'}
            />
          </Box>

          {tournament.description && (
            <Typography
              variant="body2"
              sx={{
                color: "text.secondary",
                mb: 1.5,
                display: '-webkit-box',
                WebkitLineClamp: 2,
                WebkitBoxOrient: 'vertical',
                overflow: 'hidden'
              }}>
              {tournament.description}
            </Typography>
          )}

          <Typography
            variant="caption"
            sx={{
              color: "text.secondary",
              display: "block"
            }}>
            Inicio: {formatTournamentDate(tournament.startDate)}
          </Typography>
          <Typography
            variant="caption"
            sx={{
              color: "text.secondary",
              display: "block"
            }}>
            Equipos: {tournament.minTeams}–{tournament.maxTeams}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

export default function PublicTournamentsPage() {
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const [loading, setLoading] = useState(false);
  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);

  useEffect(() => {
    getAllTournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  const fetchTournaments = useCallback(async () => {
    setLoading(true);
    await getAllTournamentsRef.current({ pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 });
    setLoading(false);
  }, []);

  useEffect(() => {
    void fetchTournaments();
  }, [fetchTournaments]);

  const rows = useMemo(() => tournaments ?? [], [tournaments]);

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Typography
        variant="h4"
        component="h1"
        sx={{
          fontWeight: "bold",
          mb: 1
        }}>
        Torneos
      </Typography>
      <Typography
        variant="body1"
        sx={{
          color: "text.secondary",
          mb: 4
        }}>
        Todos los torneos de la liga Club 12.
      </Typography>

      {loading ? (
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            py: 8
          }}>
          <CircularProgress />
        </Box>
      ) : rows.length === 0 ? (
        <Typography sx={{
          color: "text.secondary"
        }}>No hay torneos disponibles.</Typography>
      ) : (
        <Grid container spacing={3}>
          {rows.map(tournament => (
            <Grid
              key={tournament.id}
              size={{
                xs: 12,
                sm: 6,
                md: 4
              }}>
              <TournamentCard tournament={tournament} />
            </Grid>
          ))}
        </Grid>
      )}
    </Container>
  );
}
