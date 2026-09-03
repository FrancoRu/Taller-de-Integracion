import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Typography,
  Button,
  Stack,
} from '@mui/material';
import SportsBasketballIcon from '@mui/icons-material/SportsBasketball';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import {
  BlogPostResponse,
  GetBlogPostsFilteredRequest,
} from '@/modules/blogPost/type/blogPost';
import { useNavigate } from 'react-router-dom';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { BLOG_EXCERPT_LENGTH } from '@/modules/blogPost/constants/blogPost';
import LoadErrorState from '@/views/core/components/LoadErrorState';
import { CardGridSkeleton } from '@/views/core/components/skeletons';

const CARD_IMAGE_HEIGHT = 160;

const stripHtmlToExcerpt = (html: string, maxLength: number): string => {
  const withoutTags = html.replace(/<[^>]*>/g, ' ');
  const decoder = document.createElement('textarea');
  decoder.innerHTML = withoutTags;
  const text = decoder.value.replace(/\s+/g, ' ').trim();
  return text.length > maxLength ? `${text.slice(0, maxLength)}…` : text;
};

const ShowPosts: React.FC = () => {
  const { getBlogPostsByFilters } = useBlogPost();
  const [posts, setPosts] = useState<BlogPostResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);
  const [pagination, setPagination] = useState({
    page: 1,
    pageSize: TABLE_ROWS_PER_PAGE,
    totalCount: 0,
  });
  const navigate = useNavigate();

  const filterParams: GetBlogPostsFilteredRequest = useMemo(
    () => ({
      pageNumber: pagination.page,
      pageSize: pagination.pageSize,
      author: '',
      title: '',
    }),
    [pagination.page, pagination.pageSize]
  );

  const loadPosts = useCallback(async () => {
    setLoading(true);
    setError(false);
    // Suppress the global blocking alert on the initial GET; a failed load
    // returns void, which we surface as a quiet inline retry state instead.
    const response = await getBlogPostsByFilters(filterParams, { silent: true });
    if (response) {
      const { items, page, pageSize, totalCount } = response;
      setPosts(items);
      setPagination({ page, pageSize, totalCount });
    } else {
      setError(true);
    }
    setLoading(false);
  }, [filterParams, getBlogPostsByFilters]);

  useEffect(() => {
    void loadPosts();
  }, [loadPosts]);

  const handlePageChange = (direction: 'next' | 'previous') => {
    if (loading) return;
    const { page, pageSize, totalCount } = pagination;
    const newPage = direction === 'next' ? page + 1 : page - 1;

    if (newPage < 1 || newPage > Math.ceil(totalCount / pageSize)) return;
    setPagination(prev => ({ ...prev, page: newPage }));
  };

  /**
   * The list response already carries each post's full markdownText (it's
   * truncated client-side for the excerpt above), so navigating can reuse
   * the post already in memory instead of re-fetching it by id first —
   * that redundant fetch was the whole reason "Leer más" felt unresponsive,
   * since navigation only happened once it resolved.
   */
  const handleReadMore = (post: BlogPostResponse) => {
    navigate(APP_ROUTES.blogPost.build(post.slug), { state: { post } });
  };

  // Consistent with the rest of the site: a skeleton grid while loading, so the
  // empty-state message never flashes alongside a spinner during the fetch.
  if (loading) {
    return <CardGridSkeleton count={6} />;
  }

  if (error) {
    return (
      <LoadErrorState
        message="No pudimos cargar las novedades."
        onRetry={() => void loadPosts()}
      />
    );
  }

  if (posts.length === 0) {
    return (
      <Typography variant="body1" color="text.secondary">
        No hay novedades disponibles.
      </Typography>
    );
  }

  return (
    <div>
      <Grid container spacing={3} sx={{
        justifyContent: "center"
      }}>
        {posts.map(post => (
            <Grid
              key={post.id}
              size={{
                xs: 12,
                sm: 6,
                md: 4
              }}>
              <Card sx={{ maxWidth: 345, height: '100%', display: 'flex', flexDirection: 'column' }}>
                {post.photoUrl ? (
                  <CardMedia
                    component="img"
                    image={post.photoUrl}
                    alt={post.title}
                    loading="lazy"
                    sx={{
                      height: CARD_IMAGE_HEIGHT,
                      objectFit: 'cover',
                      bgcolor: 'action.hover',
                    }}
                  />
                ) : (
                  <Box
                    sx={{
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
                <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
                  <Typography variant="h6" component="h2" sx={{ mb: 1 }}>{post.title}</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
                    {stripHtmlToExcerpt(post.markdownText, BLOG_EXCERPT_LENGTH)}
                  </Typography>
                  <Button
                    variant="outlined"
                    color="primary"
                    onClick={() => handleReadMore(post)}
                    sx={{ alignSelf: 'flex-start' }}
                  >
                    Leer más
                  </Button>
                </CardContent>
              </Card>
            </Grid>
          ))}
      </Grid>

      {pagination.totalCount > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', mt: 3 }}>
          Mostrando {(pagination.page - 1) * pagination.pageSize + 1}
          –{Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} de{' '}
          {pagination.totalCount}
        </Typography>
      )}

      <Stack direction="row" spacing={2} sx={{ justifyContent: 'center', mt: 1 }}>
        {pagination.page > 1 && (
          <Button
            variant="contained"
            color="secondary"
            onClick={() => handlePageChange('previous')}
          >
            Anterior
          </Button>
        )}

        {pagination.page * pagination.pageSize < pagination.totalCount && (
          <Button
            variant="contained"
            color="primary"
            onClick={() => handlePageChange('next')}
          >
            Siguiente
          </Button>
        )}
      </Stack>
    </div>
  );
};

export default ShowPosts;
