import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BlogPostsPage from '@/views/blogPost/BlogPostsPage';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

import Swal from 'sweetalert2';

const mockedUseBlogPost = vi.mocked(useBlogPost);
const mockedSwalFire = vi.mocked(Swal.fire);

afterEach(() => {
  vi.clearAllMocks();
});

const buildPost = (overrides: Partial<BlogPostResponse> = {}): BlogPostResponse => ({
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

describe('BlogPostsPage — list actions', () => {
  it('does not offer an Editar row action — editing lives inside the post detail page', async () => {
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsByFilters: vi.fn().mockResolvedValue({
        items: [buildPost()],
        totalCount: 1,
      }),
      deleteBlogPostById: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);

    render(
      <MemoryRouter>
        <BlogPostsPage />
      </MemoryRouter>
    );

    await screen.findByText('Titulo');
    expect(screen.queryByTestId('EditIcon')).not.toBeInTheDocument();
    expect(screen.getByTestId('VisibilityIcon')).toBeInTheDocument();
  });
});

describe('BlogPostsPage — delete failure', () => {
  it('does not show a success dialog or refetch when deleteBlogPostById fails', async () => {
    const getBlogPostsByFilters = vi.fn().mockResolvedValue({
      items: [buildPost()],
      totalCount: 1,
    });
    const deleteBlogPostById = vi.fn().mockResolvedValue(false);
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsByFilters,
      deleteBlogPostById,
    } as unknown as ReturnType<typeof useBlogPost>);
    mockedSwalFire.mockResolvedValue({
      isConfirmed: true,
      isDenied: false,
      isDismissed: false,
    } as Awaited<ReturnType<typeof Swal.fire>>);

    render(
      <MemoryRouter>
        <BlogPostsPage />
      </MemoryRouter>
    );

    await screen.findByText('Titulo');
    getBlogPostsByFilters.mockClear();

    const deleteIcon = await screen.findByTestId('DeleteIcon');
    (deleteIcon.closest('button') as HTMLButtonElement).click();

    await waitFor(() => expect(deleteBlogPostById).toHaveBeenCalledTimes(1));

    // Only the confirm dialog should have fired — never a success one — and
    // the list must not refetch after a failed delete.
    expect(mockedSwalFire).not.toHaveBeenCalledWith(
      expect.objectContaining({ title: '¡Eliminada!' })
    );
    expect(getBlogPostsByFilters).not.toHaveBeenCalled();
  });
});
