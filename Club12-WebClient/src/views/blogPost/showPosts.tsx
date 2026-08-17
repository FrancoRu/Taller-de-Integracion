import React, { useEffect, useMemo, useState } from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  CircularProgress,
  Stack,
} from '@mui/material';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import {
  BlogPostResponse,
  GetBlogPostsFilteredRequest,
} from '@/modules/blogPost/type/blogPost';
import { useNavigate } from 'react-router-dom';
import { GUID } from '@/modules/core/types/types';
import { TABLE_ROWS_PER_PAGE } from '@/modules/core/constants/pagination';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { BLOG_EXCERPT_LENGTH } from '@/modules/blogPost/constants/blogPost';

const stripHtmlToExcerpt = (html: string, maxLength: number): string => {
  const withoutTags = html.replace(/<[^>]*>/g, ' ');
  const decoder = document.createElement('textarea');
  decoder.innerHTML = withoutTags;
  const text = decoder.value.replace(/\s+/g, ' ').trim();
  return text.length > maxLength ? `${text.slice(0, maxLength)}…` : text;
};

const ShowPosts: React.FC = () => {
  const { getBlogPostsByFilters, getBlogPostsById } = useBlogPost();
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

  const handleReadMore = async (id: GUID) => {
    const postDetails = await getBlogPostsById(id);
    if (postDetails) {
      navigate(APP_ROUTES.blogPost.build(id), { state: { post: postDetails } });
    }
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
                <CardContent sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
                  <Typography variant="h6" sx={{ mb: 1 }}>{post.title}</Typography>
                  <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
                    {stripHtmlToExcerpt(post.markdownText, BLOG_EXCERPT_LENGTH)}
                  </Typography>
                  <Button
                    variant="outlined"
                    color="primary"
                    onClick={() => handleReadMore(post.id)}
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
