import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Box,
  Card,
  CardActionArea,
  CardContent,
  Chip,
  Grid,
  Typography,
} from '@mui/material';
import { useSeason } from '@/modules/season/hook/season.hook';
import { ISeasonResponse } from '@/modules/season/type/season';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import PageShell from '@/views/core/components/PageShell';
import LoadErrorState from '@/views/core/components/LoadErrorState';
import { CardGridSkeleton } from '@/views/core/components/skeletons';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

export function SeasonCard({ season }: { season: ISeasonResponse }) {
  const tournamentCount = season.tournaments?.length ?? 0;

  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea
        component={Link}
        to={APP_ROUTES.publicSeason.build(season.slug ?? season.id)}
        sx={{
          height: '100%',
          alignItems: 'flex-start',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        <CardContent sx={{ width: '100%' }}>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'flex-start',
              mb: 1,
            }}
          >
            <Typography
              variant="h6"
              component="h2"
              sx={{ lineHeight: 1.3, flex: 1, mr: 1 }}
            >
              {season.name}
            </Typography>
            {season.year != null && (
              <Chip label={season.year} color="primary" size="small" />
            )}
          </Box>

          <Typography
            variant="caption"
            sx={{ color: 'text.secondary', display: 'block' }}
          >
            {tournamentCount === 1
              ? '1 torneo'
              : `${tournamentCount} torneos`}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

export default function PublicSeasonsPage() {
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: 'Temporadas',
    description:
      'Todas las temporadas de la liga Club 12: torneos, divisiones y ' +
      'resultados de cada edición.',
  });

  const { seasons, getSeasonsByFiltered } = useSeason();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const getSeasonsRef = useRef(getSeasonsByFiltered);

  useEffect(() => {
    getSeasonsRef.current = getSeasonsByFiltered;
  }, [getSeasonsByFiltered]);

  const fetchSeasons = useCallback(async () => {
    setLoading(true);
    setError(false);
    // Suppress the global blocking alert on the initial GET; a failed load
    // returns void, which we surface as a quiet inline retry state instead.
    const response = await getSeasonsRef.current(
      {
        pageSize: PUBLIC_LISTING_PAGE_SIZE,
        pageNumber: 1,
      },
      { silent: true }
    );
    setError(response === undefined);
    setLoading(false);
  }, []);

  useEffect(() => {
    void fetchSeasons();
  }, [fetchSeasons]);

  const rows = useMemo(() => seasons ?? [], [seasons]);

  return (
    <PageShell title="Temporadas">
      <Typography variant="body1" sx={{ color: 'text.secondary', mb: 4 }}>
        Todas las temporadas de la liga Club 12.
      </Typography>

      {loading ? (
        <CardGridSkeleton count={6} />
      ) : error ? (
        <LoadErrorState
          message="No pudimos cargar las temporadas."
          onRetry={() => void fetchSeasons()}
        />
      ) : rows.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          Todavía no hay temporadas publicadas. Volvé a consultar más adelante.
        </Typography>
      ) : (
        <Grid container spacing={3} component="section">
          {rows.map(season => (
            <Grid key={season.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <SeasonCard season={season} />
            </Grid>
          ))}
        </Grid>
      )}
    </PageShell>
  );
}
