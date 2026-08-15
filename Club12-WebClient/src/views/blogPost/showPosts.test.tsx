import { render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ShowPosts from '@/views/blogPost/showPosts';
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
  views: 0,
  markdownText: 'contenido',
  createdAt: new Date(),
  ...overrides,
});

const renderShowPosts = () =>
  render(
    <MemoryRouter>
      <ShowPosts />
    </MemoryRouter>
  );

describe('ShowPosts — bounded fetch on pagination changes', () => {
  let getBlogPostsByFilters: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    getBlogPostsByFilters = vi.fn();
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsByFilters,
      getBlogPostsById: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);
  });

  it('stays at one call across unrelated re-renders, then refetches once the server-confirmed pageSize changes', async () => {
    // First response echoes a pageSize different from the one requested,
    // simulating the server confirming a different page size than the client sent.
    getBlogPostsByFilters.mockResolvedValueOnce({
      items: [buildPost()],
      page: 1,
      pageSize: 25,
      totalCount: 1,
    });
    getBlogPostsByFilters.mockResolvedValue({
      items: [buildPost()],
      page: 1,
      pageSize: 25,
      totalCount: 1,
    });

    const { rerender } = renderShowPosts();

    await waitFor(() =>
      expect(getBlogPostsByFilters).toHaveBeenCalledTimes(1)
    );
    expect(getBlogPostsByFilters).toHaveBeenNthCalledWith(
      1,
      expect.objectContaining({ pageNumber: 1, pageSize: 10 })
    );

    // Force several unrelated re-render passes — must not trigger extra fetches.
    for (let i = 0; i < 3; i += 1) {
      rerender(
        <MemoryRouter>
          <ShowPosts />
        </MemoryRouter>
      );
    }

    // The pagination state now carries a different pageSize (25) than the
    // one first requested (10); the effect must refetch with the new value.
    await waitFor(() =>
      expect(getBlogPostsByFilters).toHaveBeenCalledTimes(2)
    );
    expect(getBlogPostsByFilters).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({ pageNumber: 1, pageSize: 25 })
    );
  });
});
