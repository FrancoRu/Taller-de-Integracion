import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
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
import {
  ISeasonResponse,
  ISeasonTournament,
} from '@/modules/season/type/season';
import {
  TOURNAMENT_CATEGORY_LABELS,
  TournamentCategory,
} from '@/modules/core/enum/tournament/tournamentCategory';
import { categoryColor } from '@/design/categoryColor';
import PageShell from '@/views/core/components/PageShell';
import SectionHeading from '@/views/core/components/SectionHeading';
import LoadErrorState from '@/views/core/components/LoadErrorState';
import { DetailSkeleton } from '@/views/core/components/skeletons';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

function TournamentCard({ tournament }: { tournament: ISeasonTournament }) {
  return (
    <Card sx={{ height: '100%' }}>
      <CardActionArea
        component={Link}
        to={APP_ROUTES.publicTournament.build(tournament.slug ?? tournament.id)}
        sx={{ height: '100%' }}
      >
        <CardContent sx={{ width: '100%' }}>
          <Typography variant="h6" component="h3" sx={{ lineHeight: 1.3 }}>
            {tournament.name}
          </Typography>
        </CardContent>
      </CardActionArea>
    </Card>
  );
}

function CategorySection({
  category,
  tournaments,
}: {
  category: TournamentCategory;
  tournaments: ISeasonTournament[];
}) {
  return (
    <Box component="section" sx={{ mb: 4 }}>
      <SectionHeading accentColor={categoryColor(category).fill}>
        {TOURNAMENT_CATEGORY_LABELS[category]}
      </SectionHeading>
      {tournaments.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          No hay torneos en esta categoría.
        </Typography>
      ) : (
        <Grid container spacing={3}>
          {tournaments.map(tournament => (
            <Grid key={tournament.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <TournamentCard tournament={tournament} />
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
}

export default function PublicSeasonPage() {
  const { seasonId } = useParams<{ seasonId: string }>();
  const navigate = useNavigate();
  const { getSeasonById } = useSeason();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(false);
  const [season, setSeason] = useState<ISeasonResponse | null>(null);
  const getSeasonByIdRef = useRef(getSeasonById);

  useEffect(() => {
    getSeasonByIdRef.current = getSeasonById;
  }, [getSeasonById]);

  const loadSeason = useCallback(async () => {
    if (!seasonId) {
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(false);
    // Suppress the global blocking alert on the initial GET; a failed load
    // returns void, which we surface as a quiet inline retry state instead.
    const response = await getSeasonByIdRef.current(seasonId, { silent: true });
    setSeason(response ?? null);
    setError(response === undefined);
    setLoading(false);
  }, [seasonId]);

  useEffect(() => {
    void loadSeason();
  }, [loadSeason]);

  // Set the social/SEO title from the season name once it loads; while it is
  // still undefined the hook keeps the site defaults in place.
  usePageMetadata({
    ...DEFAULT_PAGE_METADATA,
    title: season?.name,
    description: season?.name
      ? `Torneos, divisiones y resultados de ${season.name} en la liga Club 12.`
      : undefined,
  });

  if (loading) {
    return (
      <PageShell title="Temporada">
        <DetailSkeleton />
      </PageShell>
    );
  }

  if (error) {
    return (
      <PageShell
        title="Temporada"
        back={{
          label: 'Volver a temporadas',
          onClick: () => navigate(APP_ROUTES.publicSeasons),
        }}
      >
        <LoadErrorState
          message="No pudimos cargar la temporada."
          onRetry={() => void loadSeason()}
        />
      </PageShell>
    );
  }

  if (!season) {
    return (
      <PageShell
        title="Temporada no encontrada"
        back={{
          label: 'Volver a temporadas',
          onClick: () => navigate(APP_ROUTES.publicSeasons),
        }}
      >
        <Typography sx={{ color: 'text.secondary' }}>
          La temporada que buscás no existe o ya no está disponible.
        </Typography>
      </PageShell>
    );
  }

  const tournaments = season.tournaments ?? [];
  const masculineTournaments = tournaments.filter(
    t => t.category === TournamentCategory.Masculine
  );
  const feminineTournaments = tournaments.filter(
    t => t.category === TournamentCategory.Feminine
  );

  return (
    <PageShell
      back={{
        label: 'Volver a temporadas',
        onClick: () => navigate(APP_ROUTES.publicSeasons),
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 3 }}>
        <Typography variant="h4" component="h1" sx={{ fontWeight: 'bold' }}>
          {season.name}
        </Typography>
        {season.year != null && (
          <Chip label={season.year} color="primary" size="small" />
        )}
      </Box>

      {tournaments.length === 0 ? (
        <Typography sx={{ color: 'text.secondary' }}>
          Esta temporada todavía no tiene torneos asociados.
        </Typography>
      ) : (
        <>
          <CategorySection
            category={TournamentCategory.Masculine}
            tournaments={masculineTournaments}
          />
          <CategorySection
            category={TournamentCategory.Feminine}
            tournaments={feminineTournaments}
          />
        </>
      )}
    </PageShell>
  );
}
