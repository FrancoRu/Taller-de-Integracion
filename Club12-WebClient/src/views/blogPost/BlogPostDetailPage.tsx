import { useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { formatDateAr } from '@/modules/core/utils/formatDate';
import { Box, Button, Stack, Typography } from '@mui/material';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';
import PageShell from '@/views/core/components/PageShell';
import { DetailSkeleton } from '@/views/core/components/skeletons';
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
  const navigate = useNavigate();
  const { getBlogPostsById } = useBlogPost();
  const { role } = useAuth();
  const isAdminOrOwner = role === UserRolesType.Admin || role === UserRolesType.Owner;
  const seededPost = (location.state as BlogPostLocationState | undefined)?.post;
  const [post, setPost] = useState<BlogPostResponse | undefined>(seededPost);
  const [loading, setLoading] = useState(!seededPost);

  /**
   * The `idOrSlug` the currently displayed post belongs to, or undefined when
   * nothing is displayed. Deliberately a ref, not state: the fetch effect has to
   * consult it, and taking `post` as a dependency would re-run the effect on
   * every setPost — with staleTime 0 that is an unbounded fetch/increment loop.
   * Seeding it with `idOrSlug` is sound because the navigation that supplies
   * `location.state.post` builds the URL from that same post.
   */
  const routeKeyRef = useRef<string | undefined>(
    seededPost ? idOrSlug : undefined
  );

  /**
   * The `idOrSlug` the background GET has already been fired for. React
   * StrictMode (dev) mounts every component twice, and in the browser the gap
   * between the two mounts is long enough for the first GET to resolve — with
   * `staleTime` 0 the second mount then fires a SECOND GET and a second
   * `Views++` on the server (in dev the counter moved "de a 2"). Firing at most
   * once per `idOrSlug` collapses that. A real navigation unmounts this
   * component and resets the ref, so re-opening a post still counts.
   */
  const requestedForRef = useRef<string | undefined>(undefined);

  useEffect(() => {
    if (!idOrSlug || requestedForRef.current === idOrSlug) return;
    requestedForRef.current = idOrSlug;
    const requestedFor = idOrSlug;

    // The GET is the *only* thing that increments Views on the server, so it
    // must fire on the router-state path too — skipping it there is what kept
    // the "Vistas" column flat.
    //
    // COUPLING: relies on the QueryClient keeping staleTime 0
    // (QueryProvider.tsx) so fetchQuery reaches the network. A non-zero
    // staleTime — global or per-query — would serve this from cache and
    // silently stop the counter.
    //
    // Only blank the page when nothing for THIS route is on screen. When the
    // post came in via location.state the refresh runs invisibly underneath it.
    if (routeKeyRef.current !== requestedFor) setLoading(true);

    const loadPost = async () => {
      try {
        // silent: a failed refresh must not raise the global blocking alert
        // over an article the reader is already reading.
        const fetchedPost = await getBlogPostsById(requestedFor, {
          silent: true,
        });
        // A later navigation has superseded this request.
        if (requestedForRef.current !== requestedFor) return;

        if (fetchedPost) {
          setPost(fetchedPost);
          routeKeyRef.current = requestedFor;
        } else if (routeKeyRef.current !== requestedFor) {
          // Cold path only. `undefined` conflates 404 / 500 / offline, so it
          // must not tear down an article that is already readable.
          setPost(undefined);
        }
      } finally {
        if (requestedForRef.current === requestedFor) setLoading(false);
      }
    };

    void loadPost();
  }, [idOrSlug, getBlogPostsById]);

  // HU-17: set per-post Open Graph / Twitter tags so a shared blog URL renders
  // a rich card (title, description, image). Empty strings while the post is
  // still loading leave the index.html defaults in place.
  usePageMetadata({
    title: post?.title,
    description: post ? buildDescription(post.markdownText) : undefined,
    // A crawler has to be able to fetch og:image, so a locally-rendered cover
    // (a "data:" SVG) is left out and the index.html default stands.
    image: post?.photoUrl?.startsWith('http') ? post.photoUrl : undefined,
    type: 'article',
  });

  if (loading) {
    return (
      <PageShell maxWidth="md">
        <DetailSkeleton />
      </PageShell>
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
    <PageShell maxWidth="md">
      <Box component="article">
        <Stack
          direction="row"
          spacing={2}
          sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}
        >
          <Typography variant="h4" component="h1" sx={{ fontWeight: 700, mb: 1 }}>
            {post.title}
          </Typography>
          {isAdminOrOwner && (
            <Button
              variant="outlined"
              color="primary"
              onClick={() =>
                navigate(APP_ROUTES.panelBlogEdit.build(post.slug ?? post.id))
              }
              sx={{ flexShrink: 0 }}
            >
              Editar publicación
            </Button>
          )}
        </Stack>
        <Typography
          variant="subtitle1"
          component="p"
          sx={{
            color: 'text.secondary',
            mb: 3,
          }}
        >
          {post.author} · {formatDateAr(post.createdAt)}
        </Typography>
        {post.photoUrl && (
          <Box
            component="img"
            src={post.photoUrl}
            alt={post.title}
            sx={{
              display: 'block',
              width: '100%',
              maxHeight: 340,
              objectFit: 'cover',
              borderRadius: 2,
              mb: 3,
            }}
          />
        )}
        <div dangerouslySetInnerHTML={{ __html: post.markdownText }} />
      </Box>
    </PageShell>
  );
};

export default BlogPostDetailPage;
