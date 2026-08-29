import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { ITournamentResponse } from '@/modules/tournament/type/tournament';
import { TournamentStatus } from '@/modules/core/enum/tournament/tournamentStatus';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import PageShell from '@/views/core/components/PageShell';
import CategoryChip from '@/views/core/components/CategoryChip';
import LoadErrorState from '@/views/core/components/LoadErrorState';
import { CardGridSkeleton } from '@/views/core/components/skeletons';
import {
  TOURNAMENT_STATUS_LABEL,
  TOURNAMENT_STATUS_COLOR,
  formatTournamentDate,
} from '@/modules/tournament/utils/tournamentDisplay';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

export function TournamentCard({ tournament }: { tournament: ITournamentResponse }) {
  const status = tournament.status as TournamentStatus;

  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea
        component={Link}
        to={APP_ROUTES.publicTournament.build(tournament.slug ?? tournament.id)}
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
            <Stack
              direction="row"
              spacing={0.5}
              sx={{ flexShrink: 0, flexWrap: 'wrap', justifyContent: 'flex-end' }}
            >
              <CategoryChip category={tournament.category} />
              <Chip
                label={TOURNAMENT_STATUS_LABEL[status] ?? status}
                color={TOURNAMENT_STATUS_COLOR[status] ?? 'default'}
                size="small"
                variant={status === 'Scheduled' ? 'outlined' : 'filled'}
              />
            </Stack>
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
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

export default function PublicTournamentsPage() {
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: 'Torneos',
    description:
      'Todos los torneos de la liga Club 12: fechas, categorías y estado ' +
      'de cada competencia.',
  });

  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);

  useEffect(() => {
    getAllTournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  const fetchTournaments = useCallback(async () => {
    setLoading(true);
    setError(false);
    // Suppress the global blocking alert on the initial GET; a failed load
    // returns void, which we surface as a quiet inline retry state instead.
    const response = await getAllTournamentsRef.current(
      { pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 },
      { silent: true }
    );
    setError(response === undefined);
    setLoading(false);
  }, []);

  useEffect(() => {
    void fetchTournaments();
  }, [fetchTournaments]);

  const rows = useMemo(() => tournaments ?? [], [tournaments]);

  return (
    <PageShell title="Torneos">
      <Typography
        variant="body1"
        sx={{
          color: "text.secondary",
          mb: 4
        }}>
        Todos los torneos de la liga Club 12.
      </Typography>

      {loading ? (
        <CardGridSkeleton count={6} />
      ) : error ? (
        <LoadErrorState
          message="No pudimos cargar los torneos."
          onRetry={() => void fetchTournaments()}
        />
      ) : rows.length === 0 ? (
        <Typography sx={{
          color: "text.secondary"
        }}>
          Todavía no hay torneos publicados. Volvé a consultar más adelante.
        </Typography>
      ) : (
        <Grid container spacing={3} component="section">
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
    </PageShell>
  );
}
