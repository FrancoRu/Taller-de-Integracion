import { useEffect, useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { Box, CircularProgress, Typography } from '@mui/material';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';

interface BlogPostLocationState {
  post?: BlogPostResponse;
}

const BlogPostDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const { getBlogPostsById } = useBlogPost();
  const [post, setPost] = useState<BlogPostResponse | undefined>(
    (location.state as BlogPostLocationState | undefined)?.post
  );
  const [loading, setLoading] = useState(!post);

  useEffect(() => {
    if (post || !id) return;

    const loadPost = async () => {
      setLoading(true);
      try {
        const fetchedPost = await getBlogPostsById(id);
        setPost(fetchedPost ?? undefined);
      } finally {
        setLoading(false);
      }
    };

    loadPost();
  }, [id, post, getBlogPostsById]);

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!post) {
    return (
      <ErrorPageLayout code={404}>
        <Typography variant="h5" sx={{ fontWeight: 600, mb: 1 }}>
          Publicación no encontrada
        </Typography>
        <Typography variant="body1" sx={{ maxWidth: 380 }}>
          La publicación que estás buscando no existe o fue eliminada.
        </Typography>
        <ErrorPageActions />
      </ErrorPageLayout>
    );
  }

  return (
    <Box sx={{ maxWidth: 720, mx: 'auto', p: 3 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
        {post.title}
      </Typography>
      <Typography variant="subtitle1" component="p" color="text.secondary" sx={{ mb: 3 }}>
        {post.author} · {new Date(post.createdAt).toLocaleDateString()}
      </Typography>
      {post.photoUrl && (
        <Box
          component="img"
          src={post.photoUrl}
          alt={post.title}
          sx={{ width: '100%', borderRadius: 2, mb: 3 }}
        />
      )}
      <div dangerouslySetInnerHTML={{ __html: post.markdownText }} />
    </Box>
  );
};

export default BlogPostDetailPage;
