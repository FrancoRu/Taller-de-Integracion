import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import BlogPostDetailPage from '@/views/blogPost/BlogPostDetailPage';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');

const mockedUseBlogPost = vi.mocked(useBlogPost);

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

describe('BlogPostDetailPage', () => {
  it('renders the post from router state without fetching', async () => {
    const getBlogPostsById = vi.fn();
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById,
    } as unknown as ReturnType<typeof useBlogPost>);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc', {
      post: buildPost({ title: 'Post desde el estado' }),
    });

    expect(await screen.findByText('Post desde el estado')).toBeInTheDocument();
    expect(getBlogPostsById).not.toHaveBeenCalled();
  });

  it('fetches the post by id when no router state is present', async () => {
    const getBlogPostsById = vi
      .fn()
      .mockResolvedValue(buildPost({ title: 'Post recargado' }));
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById,
    } as unknown as ReturnType<typeof useBlogPost>);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc');

    expect(await screen.findByText('Post recargado')).toBeInTheDocument();
    expect(getBlogPostsById).toHaveBeenCalledWith('guid-a-aaaa-bbbb-cccc');
  });

  it('shows a not-found page when the fetched post does not exist', async () => {
    const getBlogPostsById = vi.fn().mockResolvedValue(undefined);
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById,
    } as unknown as ReturnType<typeof useBlogPost>);

    renderAt('/blog/guid-a-aaaa-bbbb-cccc');

    await waitFor(() =>
      expect(
        screen.getByText('Publicación no encontrada')
      ).toBeInTheDocument()
    );
  });
});
