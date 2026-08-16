import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Button,
  Card,
  CardActionArea,
  CardContent,
  CircularProgress,
  Container,
  Grid,
  Stack,
  Typography,
} from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import { GUID } from '@/modules/core/types/types';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';
import { TournamentCard } from '@/views/home/tournaments/PublicTournamentsPage';

const FEATURED_TOURNAMENTS_COUNT = 3;
const LATEST_POSTS_COUNT = 3;

interface QuickNavItem {
  label: string;
  description: string;
  path: string;
}

const QUICK_NAV_ITEMS: QuickNavItem[] = [
  { label: 'Torneos', description: 'Divisiones, posiciones y copa', path: APP_ROUTES.publicTournaments },
  { label: 'Partidos', description: 'Fixture y resultados', path: APP_ROUTES.publicMatches },
  { label: 'Goleadores', description: 'Ranking por división y copa', path: APP_ROUTES.publicScorers },
  { label: 'Sanciones', description: 'Suspensiones vigentes', path: APP_ROUTES.publicSanctions },
  { label: 'Equipos', description: 'Planteles de la liga', path: APP_ROUTES.publicTeams },
];

const stripHtml = (html: string) => html.replace(/<[^>]*>/g, '');

const formatPostDate = (value: Date | string) => {
  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? '' : parsed.toLocaleDateString('es-AR');
};

export default function Home() {
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

  const handleReadMore = async (id: GUID) => {
    const postDetails = await getBlogPostsById(id);
    if (postDetails) {
      navigate(APP_ROUTES.blogPost.build(id), { state: { post: postDetails } });
    }
  };

  return (
    <>
      <Box
        sx={{
          bgcolor: 'secondary.main',
          color: '#fff',
          py: { xs: 8, md: 12 },
        }}
      >
        <Container maxWidth="lg">
          <Typography variant="h2" component="h1" mb={2}>
            Club 12
          </Typography>
          <Typography variant="h6" component="p" fontWeight={400} maxWidth={640} mb={4} sx={{ opacity: 0.9 }}>
            La liga de básquet amateur con más historia de la zona. Torneos, resultados
            y estadísticas de todas las divisiones en un solo lugar.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <Button
              variant="contained"
              size="large"
              onClick={() => navigate(APP_ROUTES.publicTournaments)}
            >
              Ver torneos
            </Button>
            <Button
              variant="outlined"
              size="large"
              sx={{ color: '#fff', borderColor: '#fff', '&:hover': { borderColor: '#fff', bgcolor: 'rgba(255,255,255,0.08)' } }}
              onClick={() => navigate(APP_ROUTES.publicMatches)}
            >
              Ver partidos
            </Button>
          </Stack>
        </Container>
      </Box>

      <Container maxWidth="lg" sx={{ py: 6 }}>
        <Grid container spacing={2} mb={6}>
          {QUICK_NAV_ITEMS.map(item => (
            <Grid item xs={12} sm={6} md={2.4} key={item.path}>
              <Card sx={{ height: '100%' }}>
                <CardActionArea sx={{ height: '100%' }} onClick={() => navigate(item.path)}>
                  <CardContent>
                    <Typography variant="h6" component="h2" mb={0.5}>
                      {item.label}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {item.description}
                    </Typography>
                  </CardContent>
                </CardActionArea>
              </Card>
            </Grid>
          ))}
        </Grid>

        <Box mb={6}>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Typography variant="h4" component="h2">
              Torneos destacados
            </Typography>
            <Button onClick={() => navigate(APP_ROUTES.publicTournaments)} color="secondary">
              Ver todos
            </Button>
          </Box>

          {tournamentsLoading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : featuredTournaments.length === 0 ? (
            <Typography color="text.secondary">No hay torneos disponibles.</Typography>
          ) : (
            <Grid container spacing={3}>
              {featuredTournaments.map(tournament => (
                <Grid item xs={12} sm={6} md={4} key={tournament.id}>
                  <TournamentCard tournament={tournament} />
                </Grid>
              ))}
            </Grid>
          )}
        </Box>

        <Box>
          <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
            <Typography variant="h4" component="h2">
              Últimas noticias
            </Typography>
            <Button onClick={() => navigate(APP_ROUTES.publicBlog)} color="secondary">
              Ver todas
            </Button>
          </Box>

          {postsLoading ? (
            <Box display="flex" justifyContent="center" py={4}>
              <CircularProgress />
            </Box>
          ) : posts.length === 0 ? (
            <Typography color="text.secondary">No hay novedades por el momento.</Typography>
          ) : (
            <Grid container spacing={3}>
              {posts.map(post => (
                <Grid item xs={12} sm={6} md={4} key={post.id}>
                  <Card sx={{ height: '100%' }}>
                    <CardActionArea
                      onClick={() => void handleReadMore(post.id)}
                      sx={{ height: '100%', alignItems: 'flex-start', display: 'flex', flexDirection: 'column' }}
                    >
                      <CardContent sx={{ width: '100%' }}>
                        <Typography variant="h6" component="h3" mb={1} lineHeight={1.3}>
                          {post.title}
                        </Typography>
                        <Typography
                          variant="body2"
                          color="text.secondary"
                          mb={1.5}
                          sx={{
                            display: '-webkit-box',
                            WebkitLineClamp: 3,
                            WebkitBoxOrient: 'vertical',
                            overflow: 'hidden',
                          }}
                        >
                          {stripHtml(post.markdownText).slice(0, 160)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
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
