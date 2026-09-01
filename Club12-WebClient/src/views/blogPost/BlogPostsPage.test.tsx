import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import BlogPostsPage from '@/views/blogPost/BlogPostsPage';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('sweetalert2', () => ({ default: { fire: vi.fn() } }));

const mockedUseBlogPost = vi.mocked(useBlogPost);

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
