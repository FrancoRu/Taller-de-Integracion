import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CardMedia,
  Container,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import SportsBasketballIcon from '@mui/icons-material/SportsBasketball';
import { EmojiEventsIcon } from '@/views/core/MUI/icons/icons';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import { championService } from '@/modules/champion/service/champion.service';
import { IChampionHistory } from '@/modules/champion/type/champion.d';
import { TOURNAMENT_CATEGORY_LABELS } from '@/modules/core/enum/tournament/tournamentCategory';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { BLOG_HOME_EXCERPT_LENGTH } from '@/modules/blogPost/constants/blogPost';
import { TournamentCard } from '@/views/home/tournaments/PublicTournamentsPage';
import BasketballCourtPattern from '@/views/core/components/BasketballCourtPattern';
import PageShell from '@/views/core/components/PageShell';
import SectionHeading from '@/views/core/components/SectionHeading';
import TeamLogo from '@/views/core/components/TeamLogo';
import LoadErrorState from '@/views/core/components/LoadErrorState';
import { CardGridSkeleton } from '@/views/core/components/skeletons';
import { brand, font, logoBackground, radius } from '@/design/tokens';
import { hexToRgba } from '@/design/colorName';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

const FEATURED_TOURNAMENTS_COUNT = 3;
const LATEST_POSTS_COUNT = 3;
const RECENT_CHAMPIONS_COUNT = 4;
const CARD_IMAGE_HEIGHT = 160;

interface QuickNavItem {
  label: string;
  path: string;
}

const QUICK_NAV_ITEMS: QuickNavItem[] = [
  { label: 'Temporadas', path: APP_ROUTES.publicSeasons },
  { label: 'Sanciones', path: APP_ROUTES.publicSanctions },
  { label: 'Novedades', path: APP_ROUTES.publicBlog },
];

const stripHtml = (html: string) => {
  const withoutTags = html.replace(/<[^>]*>/g, ' ');
  const decoder = document.createElement('textarea');
  decoder.innerHTML = withoutTags;
  return decoder.value.replace(/\s+/g, ' ').trim();
};

const formatPostDate = (value: Date | string) => {
  const formatted = formatDateAr(value);
  return formatted === '—' ? '' : formatted;
};

/**
 * A compact, celebratory champion tile for the landing strip: the crowned
 * crest ringed in gold, the team name in the sporting display voice, and its
 * division/category below. The whole tile links to the full champions page.
 */
function ChampionStripCard({ entry }: { entry: IChampionHistory }) {
  const { championTeam } = entry;

  return (
    <Box
      component={Link}
      to={APP_ROUTES.publicChampions}
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        height: '100%',
        px: 1.75,
        py: 1.5,
        textDecoration: 'none',
        borderRadius: `${radius.lg}px`,
        border: `1px solid ${hexToRgba(brand.gold, 0.4)}`,
        bgcolor: hexToRgba(brand.gold, 0.06),
        color: 'text.primary',
        transition: 'transform 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease',
        '&:hover': {
          borderColor: brand.gold,
          transform: 'translateY(-2px)',
          boxShadow: `0 10px 28px -14px ${hexToRgba(brand.gold, 0.7)}`,
        },
        '&:focus-visible': {
          outline: `2px solid ${brand.gold}`,
          outlineOffset: 2,
        },
        '@media (prefers-reduced-motion: reduce)': {
          transition: 'none',
          '&:hover': { transform: 'none' },
        },
      }}
    >
      {/* Crowned crest: a gold ring over a soft gold glow. */}
      <Box sx={{ position: 'relative', display: 'inline-flex', flexShrink: 0 }}>
        <Box
          aria-hidden
          sx={{
            position: 'absolute',
            inset: -6,
            borderRadius: '50%',
            background: `radial-gradient(circle, ${hexToRgba(brand.gold, 0.28)} 0%, transparent 70%)`,
          }}
        />
        <Box
          sx={{
            position: 'relative',
            borderRadius: '50%',
            p: '2px',
            border: `2px solid ${brand.gold}`,
          }}
        >
          <TeamLogo
            teamName={championTeam.teamName}
            logoUrl={championTeam.logoUrl}
            size={44}
          />
        </Box>
      </Box>

      <Box sx={{ minWidth: 0 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
          <EmojiEventsIcon sx={{ fontSize: 16, color: brand.gold, flexShrink: 0 }} />
          <Typography
            component="span"
            noWrap
            sx={{
              fontFamily: font.display,
              fontWeight: 700,
              fontSize: '1rem',
              lineHeight: 1.15,
              textTransform: 'uppercase',
              letterSpacing: '0.01em',
            }}
          >
            {championTeam.teamName}
          </Typography>
        </Box>
        <Typography
          variant="caption"
          noWrap
          sx={{ color: 'text.secondary', display: 'block' }}
        >
          {entry.divisionName} · {TOURNAMENT_CATEGORY_LABELS[entry.category]}
        </Typography>
      </Box>
    </Box>
  );
}

export default function Home() {
  // HU-17: sensible default social/SEO metadata for the landing page.
  usePageMetadata(DEFAULT_PAGE_METADATA);

  const navigate = useNavigate();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { getBlogPostsByFilters, getBlogPostsById } = useBlogPost();
  const [posts, setPosts] = useState<BlogPostResponse[]>([]);
  const [champions, setChampions] = useState<IChampionHistory[]>([]);
  const [postsLoading, setPostsLoading] = useState(false);
  const [tournamentsLoading, setTournamentsLoading] = useState(false);
  const [postsError, setPostsError] = useState(false);
  const [tournamentsError, setTournamentsError] = useState(false);

  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);
  const getBlogPostsRef = useRef(getBlogPostsByFilters);
  const getChampionsHistoryRef = useRef(championService.getChampionsHistory);

  useEffect(() => { getAllTournamentsRef.current = getAllTournamentsByFilter; }, [getAllTournamentsByFilter]);
  useEffect(() => { getBlogPostsRef.current = getBlogPostsByFilters; }, [getBlogPostsByFilters]);

  // The landing sections fetch on mount but must NOT pop the global blocking
  // alert if a GET fails — each degrades to its own quiet inline retry state.
  const fetchTournaments = useCallback(async () => {
    setTournamentsLoading(true);
    setTournamentsError(false);
    const response = await getAllTournamentsRef.current(
      { pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 },
      { silent: true }
    );
    setTournamentsError(response === undefined);
    setTournamentsLoading(false);
  }, []);

  const fetchPosts = useCallback(async () => {
    setPostsLoading(true);
    setPostsError(false);
    const response = await getBlogPostsRef.current(
      {
        pageNumber: 1,
        pageSize: LATEST_POSTS_COUNT,
        author: '',
        title: '',
      },
      { silent: true }
    );
    setPostsError(response === undefined);
    setPosts(response?.items ?? []);
    setPostsLoading(false);
  }, []);

  useEffect(() => {
    void fetchTournaments();
  }, [fetchTournaments]);

  useEffect(() => {
    void fetchPosts();
  }, [fetchPosts]);

  useEffect(() => {
    let cancelled = false;
    const fetchChampions = async () => {
      try {
        const response = await getChampionsHistoryRef.current();
        if (!cancelled) setChampions(response.data ?? []);
      } catch {
        if (!cancelled) setChampions([]);
      }
    };
    void fetchChampions();
    return () => {
      cancelled = true;
    };
  }, []);

  const featuredTournaments = useMemo(
    () => (tournaments ?? []).slice(0, FEATURED_TOURNAMENTS_COUNT),
    [tournaments]
  );

  const recentChampions = useMemo(
    () => champions.slice(0, RECENT_CHAMPIONS_COUNT),
    [champions]
  );

  const handleReadMore = async (idOrSlug: string) => {
    const postDetails = await getBlogPostsById(idOrSlug);
    if (postDetails) {
      navigate(APP_ROUTES.blogPost.build(idOrSlug), { state: { post: postDetails } });
    }
  };

  return (
    <>
      {/* Hero — the thesis: warm gradient, court texture, the club's voice. */}
      <Box
        component="section"
        sx={{
          position: 'relative',
          overflow: 'hidden',
          color: '#fff',
          background: `linear-gradient(135deg, ${brand.orange} 0%, ${brand.orangeDark} 55%, ${logoBackground} 100%)`,
          py: { xs: 9, md: 14 },
        }}
      >
        <BasketballCourtPattern
          sx={{ color: '#fff', opacity: 0.07, strokeWidth: 3 }}
        />
        {/* Legibility scrim: darkens the warm-but-bright left edge so white text
            stays AA-legible over the orange. */}
        <Box
          aria-hidden
          sx={{
            position: 'absolute',
            inset: 0,
            background:
              'linear-gradient(90deg, rgba(0,0,0,0.55) 0%, rgba(0,0,0,0.18) 55%, rgba(0,0,0,0) 100%)',
          }}
        />

        <Container maxWidth="lg" sx={{ position: 'relative' }}>
          <Typography
            component="p"
            sx={{
              fontFamily: font.display,
              fontWeight: 600,
              textTransform: 'uppercase',
              letterSpacing: '0.18em',
              fontSize: { xs: '0.8rem', md: '0.95rem' },
              color: brand.goldLight,
              mb: 1.5,
            }}
          >
            La primera liga libre de básquet
          </Typography>
          <Typography
            component="h1"
            sx={{
              fontFamily: font.display,
              fontWeight: 700,
              textTransform: 'uppercase',
              letterSpacing: '0.01em',
              lineHeight: 0.95,
              fontSize: { xs: '3.5rem', sm: '4.75rem', md: '6.5rem' },
              mb: 2,
              textShadow: '0 2px 24px rgba(0,0,0,0.35)',
            }}
          >
            Club 12
          </Typography>
          <Typography
            component="p"
            sx={{
              fontWeight: 400,
              maxWidth: 640,
              mb: 4,
              fontSize: { xs: '1.05rem', md: '1.25rem' },
              color: 'rgba(255,255,255,0.94)',
            }}
          >
            Paraná, Entre Ríos · Masculino y Femenino. Torneos, resultados y
            estadísticas de todas las divisiones en un solo lugar.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Button
              variant="contained"
              size="large"
              component={Link}
              to={APP_ROUTES.publicSeasons}
            >
              Ver temporadas
            </Button>
            <Button
              variant="outlined"
              size="large"
              color="inherit"
              startIcon={<EmojiEventsIcon />}
              component={Link}
              to={APP_ROUTES.publicChampions}
              sx={{
                borderColor: 'rgba(255,255,255,0.7)',
                color: '#fff',
                '&:hover': {
                  borderColor: '#fff',
                  bgcolor: 'rgba(255,255,255,0.12)',
                },
              }}
            >
              Campeones
            </Button>
          </Stack>
        </Container>
      </Box>

      <PageShell>
        {/* Torneos destacados */}
        <Box component="section" sx={{ mb: 6 }}>
          <SectionHeading
            component="h2"
            action={
              <Button component={Link} to={APP_ROUTES.publicSeasons} color="primary">
                Ver temporadas
              </Button>
            }
          >
            Torneos destacados
          </SectionHeading>

          {tournamentsLoading ? (
            <CardGridSkeleton count={3} />
          ) : tournamentsError ? (
            <LoadErrorState
              message="No pudimos cargar los torneos."
              onRetry={() => void fetchTournaments()}
            />
          ) : featuredTournaments.length === 0 ? (
            <Typography sx={{ color: 'text.secondary' }}>
              Todavía no hay torneos publicados. Volvé a consultar más adelante.
            </Typography>
          ) : (
            <Grid container spacing={3}>
              {featuredTournaments.map(tournament => (
                <Grid
                  key={tournament.id}
                  size={{ xs: 12, sm: 6, md: 4 }}>
                  <TournamentCard tournament={tournament} />
                </Grid>
              ))}
            </Grid>
          )}
        </Box>

        {/* Campeones recientes — hidden entirely when there are none. */}
        {recentChampions.length > 0 && (
          <Box component="section" sx={{ mb: 6 }}>
            <SectionHeading
              component="h2"
              accentColor={brand.gold}
              action={
                <Button
                  component={Link}
                  to={APP_ROUTES.publicChampions}
                  sx={{ color: brand.gold, '&:hover': { color: brand.goldLight } }}
                >
                  Ver campeones
                </Button>
              }
            >
              Campeones recientes
            </SectionHeading>

            <Grid container spacing={2}>
              {recentChampions.map(entry => (
                <Grid
                  key={`${entry.tournamentId}-${entry.divisionName}`}
                  size={{ xs: 12, sm: 6, md: 3 }}>
                  <ChampionStripCard entry={entry} />
                </Grid>
              ))}
            </Grid>
          </Box>
        )}

        {/* Últimas noticias */}
        <Box component="section" sx={{ mb: 6 }}>
          <SectionHeading
            component="h2"
            action={
              <Button component={Link} to={APP_ROUTES.publicBlog} color="primary">
                Ver todas
              </Button>
            }
          >
            Últimas noticias
          </SectionHeading>

          {postsLoading ? (
            <CardGridSkeleton count={3} />
          ) : postsError ? (
            <LoadErrorState
              message="No pudimos cargar las novedades."
              onRetry={() => void fetchPosts()}
            />
          ) : posts.length === 0 ? (
            <Typography sx={{ color: 'text.secondary' }}>
              Todavía no publicamos novedades. Pronto vas a encontrar noticias acá.
            </Typography>
          ) : (
            <Grid container spacing={3}>
              {posts.map(post => (
                <Grid
                  key={post.id}
                  size={{ xs: 12, sm: 6, md: 4 }}>
                  <Card component="article" sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                    <CardActionArea
                      onClick={() => void handleReadMore(post.slug)}
                      sx={{ height: '100%', alignItems: 'flex-start', display: 'flex', flexDirection: 'column' }}
                    >
                      {post.photoUrl ? (
                        <CardMedia
                          component="img"
                          image={post.photoUrl}
                          alt={post.title}
                          loading="lazy"
                          sx={{
                            width: '100%',
                            height: CARD_IMAGE_HEIGHT,
                            objectFit: 'cover',
                            bgcolor: 'action.hover',
                          }}
                        />
                      ) : (
                        <Box
                          sx={{
                            width: '100%',
                            height: CARD_IMAGE_HEIGHT,
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'center',
                            bgcolor: 'action.hover',
                            color: 'text.disabled',
                          }}
                        >
                          <SportsBasketballIcon sx={{ fontSize: 48 }} />
                        </Box>
                      )}
                      <CardContent sx={{ width: '100%' }}>
                        <Typography
                          variant="h6"
                          component="h3"
                          sx={{ mb: 1, lineHeight: 1.3 }}>
                          {post.title}
                        </Typography>
                        <Typography
                          variant="body2"
                          sx={{
                            color: 'text.secondary',
                            mb: 1.5,
                            display: '-webkit-box',
                            WebkitLineClamp: 3,
                            WebkitBoxOrient: 'vertical',
                            overflow: 'hidden'
                          }}>
                          {stripHtml(post.markdownText).slice(0, BLOG_HOME_EXCERPT_LENGTH)}
                        </Typography>
                        <Typography variant="caption" sx={{ color: 'text.secondary' }}>
                          {post.author} · {formatPostDate(post.createdAt)}
                        </Typography>
                      </CardContent>
                    </CardActionArea>
                  </Card>
                </Grid>
              ))}
            </Grid>
          )}
        </Box>

        {/* Accesos rápidos — a light secondary strip, not competing with the hero. */}
        <Box
          component="nav"
          aria-label="Accesos rápidos"
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            gap: 1,
            pt: 3,
            borderTop: '1px solid',
            borderColor: 'divider',
          }}
        >
          <Typography
            component="span"
            variant="overline"
            sx={{ color: 'text.secondary', mr: 1 }}
          >
            Accesos rápidos
          </Typography>
          {QUICK_NAV_ITEMS.map(item => (
            <Button
              key={item.path}
              component={Link}
              to={item.path}
              size="small"
              variant="outlined"
              color="secondary"
              sx={{ borderRadius: `${radius.pill}px` }}
            >
              {item.label}
            </Button>
          ))}
        </Box>
      </PageShell>
    </>
  );
}
