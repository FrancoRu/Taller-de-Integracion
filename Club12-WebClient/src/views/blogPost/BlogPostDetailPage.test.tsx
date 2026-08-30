import { act, render, screen, waitFor } from '@testing-library/react';
import { StrictMode } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BlogPostDetailPage from '@/views/blogPost/BlogPostDetailPage';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { BlogPostProvider } from '@/modules/blogPost/context/blogPost.context';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { sendGet } from '@/modules/core/utils/axiosUtils';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('@/modules/core/utils/axiosUtils', () => ({
  sendGet: vi.fn(),
  sendPost: vi.fn(),
  sendPut: vi.fn(),
  sendDelete: vi.fn(),
}));
vi.mock('sweetalert2', () => ({
  default: { fire: vi.fn() },
}));

const mockedUseBlogPost = vi.mocked(useBlogPost);
const mockedSendGet = vi.mocked(sendGet);

/**
 * A promise whose resolution is controlled by the test, so the in-flight
 * window of the background GET is inspectable instead of racing `waitFor`.
 */
const deferred = <T,>(): {
  promise: Promise<T>;
  resolve: (value: T) => void;
} => {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>(r => {
    resolve = r;
  });
  return { promise, resolve };
};

const buildPost = (
  overrides: Partial<BlogPostResponse> = {}
): BlogPostResponse => ({
  id: 'guid-a-aaaa-bbbb-cccc' as unknown as GUID,
  author: 'Autor',
  title: 'Titulo',
  slug: 'titulo',
  views: 0,
  markdownText: 'contenido',
  createdAt: new Date('2026-01-01'),
  isPublished: true,
  ...overrides,
});

const renderAt = (path: string, state?: unknown) =>
  render(
    <MemoryRouter initialEntries={[{ pathname: path, state }]}>
      <Routes>
        <Route path="/blog/:idOrSlug" element={<BlogPostDetailPage />} />
      </Routes>
    </MemoryRouter>
  );

const withGetBlogPostsById = (getBlogPostsById: ReturnType<typeof vi.fn>) => {
  mockedUseBlogPost.mockReturnValue({
    getBlogPostsById,
  } as unknown as ReturnType<typeof useBlogPost>);
};

afterEach(() => {
  vi.clearAllMocks();
});

describe('BlogPostDetailPage', () => {
  it('renders the post from router state and still fires exactly one background GET', async () => {
    const pending = deferred<BlogPostResponse>();
    const getBlogPostsById = vi.fn().mockReturnValue(pending.promise);
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/mi-slug', {
      post: buildPost({ slug: 'mi-slug', title: 'Post desde el estado' }),
    });

    expect(screen.getByText('Post desde el estado')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();

    await waitFor(() =>
      expect(getBlogPostsById).toHaveBeenCalledTimes(1)
    );
    expect(getBlogPostsById).toHaveBeenCalledWith('mi-slug', { silent: true });

    await act(async () => {
      pending.resolve(
        buildPost({ slug: 'mi-slug', title: 'Post desde el estado' })
      );
    });
  });

  it('replaces the state post with the fetched copy on resolve', async () => {
    const pending = deferred<BlogPostResponse>();
    const getBlogPostsById = vi.fn().mockReturnValue(pending.promise);
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/mi-slug', {
      post: buildPost({ slug: 'mi-slug', title: 'Post desde el estado' }),
    });

    expect(screen.getByText('Post desde el estado')).toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();

    await act(async () => {
      pending.resolve(
        buildPost({ slug: 'mi-slug', title: 'Post del servidor', views: 8 })
      );
    });

    expect(await screen.findByText('Post del servidor')).toBeInTheDocument();
    expect(screen.queryByText('Post desde el estado')).not.toBeInTheDocument();
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('keeps showing the state post and raises no alert when the background GET fails', async () => {
    const Swal = (await import('sweetalert2')).default;
    const getBlogPostsById = vi.fn().mockResolvedValue(undefined);
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/mi-slug', {
      post: buildPost({ slug: 'mi-slug', title: 'Post desde el estado' }),
    });

    await waitFor(() =>
      expect(getBlogPostsById).toHaveBeenCalledWith('mi-slug', { silent: true })
    );

    expect(await screen.findByText('Post desde el estado')).toBeInTheDocument();
    expect(
      screen.queryByText('Publicación no encontrada')
    ).not.toBeInTheDocument();
    expect(Swal.fire).not.toHaveBeenCalled();
  });

  it('fetches the post by id when no router state is present', async () => {
    const getBlogPostsById = vi
      .fn()
      .mockResolvedValue(buildPost({ title: 'Post recargado' }));
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc');

    expect(await screen.findByText('Post recargado')).toBeInTheDocument();
    expect(getBlogPostsById).toHaveBeenCalledWith('guid-a-aaaa-bbbb-cccc', {
      silent: true,
    });
    expect(getBlogPostsById).toHaveBeenCalledTimes(1);
  });

  it('shows a not-found page when the fetched post does not exist', async () => {
    const getBlogPostsById = vi.fn().mockResolvedValue(undefined);
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc');

    await waitFor(() =>
      expect(screen.getByText('Publicación no encontrada')).toBeInTheDocument()
    );
  });

  it('shows the skeleton while the cold fetch is in flight', async () => {
    const pending = deferred<BlogPostResponse>();
    const getBlogPostsById = vi.fn().mockReturnValue(pending.promise);
    withGetBlogPostsById(getBlogPostsById);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc');

    expect(screen.getByRole('status')).toBeInTheDocument();

    await act(async () => {
      pending.resolve(buildPost({ title: 'Post recargado' }));
    });

    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('fires the background GET only once under a StrictMode double mount', async () => {
    const getBlogPostsById = vi
      .fn()
      .mockResolvedValue(
        buildPost({ slug: 'mi-slug', title: 'Post desde el estado' })
      );
    withGetBlogPostsById(getBlogPostsById);

    render(
      <StrictMode>
        <MemoryRouter
          initialEntries={[
            {
              pathname: '/blog/mi-slug',
              state: {
                post: buildPost({
                  slug: 'mi-slug',
                  title: 'Post desde el estado',
                }),
              },
            },
          ]}
        >
          <Routes>
            <Route path="/blog/:idOrSlug" element={<BlogPostDetailPage />} />
          </Routes>
        </MemoryRouter>
      </StrictMode>
    );

    await act(async () => {
      await Promise.resolve();
    });

    expect(getBlogPostsById).toHaveBeenCalledTimes(1);
    expect(getBlogPostsById).toHaveBeenCalledWith('mi-slug', { silent: true });
  });

  it('fires the background GET again after a real remount for the same slug', async () => {
    const getBlogPostsById = vi
      .fn()
      .mockResolvedValue(buildPost({ slug: 'mi-slug' }));
    withGetBlogPostsById(getBlogPostsById);

    const { unmount } = renderAt('/blog/mi-slug', {
      post: buildPost({ slug: 'mi-slug' }),
    });
    await act(async () => {
      await Promise.resolve();
    });
    unmount();

    renderAt('/blog/mi-slug', { post: buildPost({ slug: 'mi-slug' }) });
    await act(async () => {
      await Promise.resolve();
    });

    expect(getBlogPostsById).toHaveBeenCalledTimes(2);
  });
});

describe('BlogPostDetailPage — network boundary', () => {
  beforeEach(async () => {
    const actual = await vi.importActual<
      typeof import('@/modules/blogPost/hook/blogPost.hook')
    >('@/modules/blogPost/hook/blogPost.hook');
    mockedUseBlogPost.mockImplementation(actual.useBlogPost);
  });

  const renderWithRealProviders = (client: QueryClient) =>
    render(
      <StrictMode>
        <QueryClientProvider client={client}>
          <ErrorProvider>
            <BlogPostProvider>
              <MemoryRouter
                initialEntries={[
                  {
                    pathname: '/blog/mi-slug',
                    state: { post: buildPost({ slug: 'mi-slug' }) },
                  },
                ]}
              >
                <Routes>
                  <Route
                    path="/blog/:idOrSlug"
                    element={<BlogPostDetailPage />}
                  />
                </Routes>
              </MemoryRouter>
            </BlogPostProvider>
          </ErrorProvider>
        </QueryClientProvider>
      </StrictMode>
    );

  it('issues one network GET per mount, even across remounts', async () => {
    mockedSendGet.mockResolvedValue({
      data: buildPost({ slug: 'mi-slug' }),
    } as Awaited<ReturnType<typeof sendGet>>);
    const client = new QueryClient();

    const { unmount } = renderWithRealProviders(client);
    await waitFor(() => expect(mockedSendGet).toHaveBeenCalledTimes(1));

    unmount();

    renderWithRealProviders(client);
    await waitFor(() => expect(mockedSendGet).toHaveBeenCalledTimes(2));
  });
});
