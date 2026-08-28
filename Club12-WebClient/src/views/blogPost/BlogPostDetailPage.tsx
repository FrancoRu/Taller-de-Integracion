import { useEffect, useState } from 'react';
import { useLocation, useParams } from 'react-router-dom';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import { Box, CircularProgress, Typography } from '@mui/material';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';
import { usePageMetadata } from '@/modules/core/utils/pageMetadata';

const BLOG_META_DESCRIPTION_LENGTH = 200;

/** Reduces the post's HTML body to a plain-text excerpt for social cards. */
const buildDescription = (html: string): string => {
  const withoutTags = html.replace(/<[^>]*>/g, ' ');
  const decoder = document.createElement('textarea');
  decoder.innerHTML = withoutTags;
  const text = decoder.value.replace(/\s+/g, ' ').trim();
  return text.length > BLOG_META_DESCRIPTION_LENGTH
    ? `${text.slice(0, BLOG_META_DESCRIPTION_LENGTH)}…`
    : text;
};

interface BlogPostLocationState {
  post?: BlogPostResponse;
}

const BlogPostDetailPage: React.FC = () => {
  const { idOrSlug } = useParams<{ idOrSlug: string }>();
  const location = useLocation();
  const { getBlogPostsById } = useBlogPost();
  const [post, setPost] = useState<BlogPostResponse | undefined>(
    (location.state as BlogPostLocationState | undefined)?.post
  );
  const [loading, setLoading] = useState(!post);

  useEffect(() => {
    if (post || !idOrSlug) return;

    const loadPost = async () => {
      setLoading(true);
      try {
        const fetchedPost = await getBlogPostsById(idOrSlug);
        setPost(fetchedPost ?? undefined);
      } finally {
        setLoading(false);
      }
    };

    loadPost();
  }, [idOrSlug, post, getBlogPostsById]);

  // HU-17: set per-post Open Graph / Twitter tags so a shared blog URL renders
  // a rich card (title, description, image). Empty strings while the post is
  // still loading leave the index.html defaults in place.
  usePageMetadata({
    title: post?.title,
    description: post ? buildDescription(post.markdownText) : undefined,
    image: post?.photoUrl,
    type: 'article',
  });

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
      <Typography
        variant="subtitle1"
        component="p"
        sx={{
          color: "text.secondary",
          mb: 3
        }}>
        {post.author} · {formatDateAr(post.createdAt)}
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
