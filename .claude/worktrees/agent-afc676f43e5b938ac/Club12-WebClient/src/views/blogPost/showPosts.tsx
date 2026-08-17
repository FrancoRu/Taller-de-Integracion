import React, { useEffect, useMemo, useState } from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Typography,
  Button,
  CircularProgress,
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

  useEffect(() => {
    const loadPosts = async () => {
      setLoading(true);
      try {
        const response = await getBlogPostsByFilters(filterParams);
        if (response) {
          const { items, page, pageSize, totalCount } = response;
          setPosts(items);
          setPagination({ page, pageSize, totalCount });
        }
      } finally {
        setLoading(false);
      }
    };
    void loadPosts();
  }, [filterParams, getBlogPostsByFilters]);

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
    navigate(APP_ROUTES.blogPost.build(post.id), { state: { post } });
  };

  return (
    <div>
      <Grid container spacing={3} sx={{
        justifyContent: "center"
      }}>
        {posts.length > 0 ? (
          posts.map(post => (
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
                  <Typography variant="h6" sx={{ mb: 1 }}>{post.title}</Typography>
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
          ))
        ) : (
          <Typography variant="body1" color="text.secondary">
            No hay novedades disponibles.
          </Typography>
        )}
      </Grid>

      {!loading && pagination.totalCount > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', mt: 3 }}>
          Mostrando {(pagination.page - 1) * pagination.pageSize + 1}
          –{Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} de{' '}
          {pagination.totalCount}
        </Typography>
      )}

      <Stack direction="row" spacing={2} sx={{ justifyContent: 'center', mt: 1 }}>
        {loading ? (
          <CircularProgress />
        ) : (
          <>
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
          </>
        )}
      </Stack>
    </div>
  );
};

export default ShowPosts;
