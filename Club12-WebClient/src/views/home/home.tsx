import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CardMedia,
  CircularProgress,
  Container,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import SportsBasketballIcon from '@mui/icons-material/SportsBasketball';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { BLOG_HOME_EXCERPT_LENGTH } from '@/modules/blogPost/constants/blogPost';
import { TournamentCard } from '@/views/home/tournaments/PublicTournamentsPage';
import BasketballCourtPattern from '@/views/core/components/BasketballCourtPattern';
import {
  DEFAULT_PAGE_METADATA,
  usePageMetadata,
} from '@/modules/core/utils/pageMetadata';

const FEATURED_TOURNAMENTS_COUNT = 3;
const LATEST_POSTS_COUNT = 3;
const CARD_IMAGE_HEIGHT = 160;

interface QuickNavItem {
  label: string;
  description: string;
  path: string;
}

const QUICK_NAV_ITEMS: QuickNavItem[] = [
  { label: 'Torneos', description: 'Divisiones, posiciones, goleadores, partidos y copa', path: APP_ROUTES.publicTournaments },
  { label: 'Sanciones', description: 'Suspensiones vigentes', path: APP_ROUTES.publicSanctions },
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

export default function Home() {
  // HU-17: sensible default social/SEO metadata for the landing page.
  usePageMetadata(DEFAULT_PAGE_METADATA);

  const navigate = useNavigate();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const { getBlogPostsByFilters, getBlogPostsById } = useBlogPost();
  const [posts, setPosts] = useState<BlogPostResponse[]>([]);
  const [postsLoading, setPostsLoading] = useState(false);
  const [tournamentsLoading, setTournamentsLoading] = useState(false);

  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);
  const getBlogPostsRef = useRef(getBlogPostsByFilters);

  useEffect(() => { getAllTournamentsRef.current = getAllTournamentsByFilter; }, [getAllTournamentsByFilter]);
  useEffect(() => { getBlogPostsRef.current = getBlogPostsByFilters; }, [getBlogPostsByFilters]);

  useEffect(() => {
    const fetchTournaments = async () => {
      setTournamentsLoading(true);
      await getAllTournamentsRef.current({ pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 });
      setTournamentsLoading(false);
    };
    void fetchTournaments();
  }, []);

  useEffect(() => {
    const fetchPosts = async () => {
      setPostsLoading(true);
      const response = await getBlogPostsRef.current({
        pageNumber: 1,
        pageSize: LATEST_POSTS_COUNT,
        author: '',
        title: '',
      });
      setPosts(response?.items ?? []);
      setPostsLoading(false);
    };
    void fetchPosts();
  }, []);

  const featuredTournaments = useMemo(
    () => (tournaments ?? []).slice(0, FEATURED_TOURNAMENTS_COUNT),
    [tournaments]
  );

  const handleReadMore = async (idOrSlug: string) => {
    const postDetails = await getBlogPostsById(idOrSlug);
    if (postDetails) {
      navigate(APP_ROUTES.blogPost.build(idOrSlug), { state: { post: postDetails } });
    }
  };

  return (
    <>
      <Box
        sx={{
          position: 'relative',
          overflow: 'hidden',
          bgcolor: 'secondary.main',
          color: '#fff',
          py: { xs: 8, md: 12 },
        }}
      >
        <BasketballCourtPattern
          sx={{ color: 'primary.main', opacity: 0.12, strokeWidth: 3 }}
        />
        <SportsBasketballIcon
          sx={{
            position: 'absolute',
            top: { xs: -60, md: -80 },
            right: { xs: -60, md: -40 },
            fontSize: { xs: 240, md: 340 },
            color: 'primary.main',
            opacity: 0.16,
            transform: 'rotate(18deg)',
          }}
        />

        <Container maxWidth="lg" sx={{ position: 'relative' }}>
          <Typography variant="h2" component="h1" sx={{
            mb: 2
          }}>
            Club 12
          </Typography>
          <Typography
            variant="h6"
            component="p"
            sx={{
              fontWeight: 400,
              maxWidth: 640,
              mb: 4,
              opacity: 0.9
            }}>
            La liga de básquet amateur con más historia de la zona. Torneos, resultados
            y estadísticas de todas las divisiones en un solo lugar.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Button
              variant="contained"
              size="large"
              component={Link}
              to={APP_ROUTES.publicTournaments}
            >
              Ver torneos
            </Button>
          </Stack>
        </Container>
      </Box>

      <Container maxWidth="lg" sx={{ py: 6 }}>
        <Grid container spacing={2} sx={{
          mb: 6
        }}>
          {QUICK_NAV_ITEMS.map(item => (
            <Grid
              key={item.path}
              size={{
                xs: 12,
                sm: 6
              }}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea sx={{ height: '100%' }} component={Link} to={item.path}>
                  <CardContent>
                    <Typography variant="h6" component="h2" sx={{
                      mb: 0.5
                    }}>
                      {item.label}
                    </Typography>
                    <Typography variant="body2" sx={{
                      color: "text.secondary"
                    }}>
                      {item.description}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>

        <Box sx={{
          mb: 6
        }}>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              mb: 3
            }}>
            <Typography variant="h4" component="h2">
              Torneos destacados
            </Typography>
            <Button component={Link} to={APP_ROUTES.publicTournaments} color="primary">
              Ver todos
            </Button>
          </Box>

          {tournamentsLoading ? (
            <Box
              sx={{
                display: "flex",
                justifyContent: "center",
                py: 4
              }}>
              <CircularProgress />
            </Box>
          ) : featuredTournaments.length === 0 ? (
            <Typography sx={{
              color: "text.secondary"
            }}>No hay torneos disponibles.</Typography>
          ) : (
            <Grid container spacing={3}>
              {featuredTournaments.map(tournament => (
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
        </Box>

        <Box>
          <Box
            sx={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              mb: 3
            }}>
            <Typography variant="h4" component="h2">
              Últimas noticias
            </Typography>
            <Button component={Link} to={APP_ROUTES.publicBlog} color="primary">
              Ver todas
            </Button>
          </Box>

          {postsLoading ? (
            <Box
              sx={{
                display: "flex",
                justifyContent: "center",
                py: 4
              }}>
              <CircularProgress />
            </Box>
          ) : posts.length === 0 ? (
            <Typography sx={{
              color: "text.secondary"
            }}>No hay novedades por el momento.</Typography>
          ) : (
            <Grid container spacing={3}>
              {posts.map(post => (
                <Grid
                  key={post.id}
                  size={{
                    xs: 12,
                    sm: 6,
                    md: 4
                  }}>
                  <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
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
                          sx={{
                            mb: 1,
                            lineHeight: 1.3
                          }}>
                          {post.title}
                        </Typography>
                        <Typography
                          variant="body2"
                          sx={{
                            color: "text.secondary",
                            mb: 1.5,
                            display: '-webkit-box',
                            WebkitLineClamp: 3,
                            WebkitBoxOrient: 'vertical',
                            overflow: 'hidden'
                          }}>
                          {stripHtml(post.markdownText).slice(0, BLOG_HOME_EXCERPT_LENGTH)}
                        </Typography>
                        <Typography variant="caption" sx={{
                          color: "text.secondary"
                        }}>
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
      </Container>
    </>
  );
}
