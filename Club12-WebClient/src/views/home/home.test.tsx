import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import Home from '@/views/home/home';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { useSeason } from '@/modules/season/hook/season.hook';
import { championService } from '@/modules/champion/service/champion.service';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('@/modules/season/hook/season.hook');
vi.mock('@/modules/champion/service/champion.service', () => ({
  championService: { getChampionsHistory: vi.fn() },
}));

const navigateSpy = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual =
    await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
  return { ...actual, useNavigate: () => navigateSpy };
});

const mockedUseBlogPost = vi.mocked(useBlogPost);
const mockedUseSeason = vi.mocked(useSeason);
const mockedGetChampionsHistory = vi.mocked(championService.getChampionsHistory);

const buildPost = (
  overrides: Partial<BlogPostResponse> = {}
): BlogPostResponse => ({
  id: 'guid-a-aaaa-bbbb-cccc' as unknown as GUID,
  author: 'Club 12',
  title: 'Para cerrar el año',
  slug: 'para-cerrar-el-ano',
  views: 5,
  markdownText: '<p>cuerpo de la noticia</p>',
  createdAt: new Date('2026-01-01'),
  isPublished: true,
  ...overrides,
});

let getBlogPostsById: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.clearAllMocks();

  getBlogPostsById = vi.fn().mockResolvedValue(buildPost());
  mockedUseBlogPost.mockReturnValue({
    getBlogPostsByFilters: vi.fn().mockResolvedValue({
      items: [buildPost()],
      page: 1,
      pageSize: 3,
      totalCount: 1,
    }),
    getBlogPostsById,
  } as unknown as ReturnType<typeof useBlogPost>);

  mockedUseSeason.mockReturnValue({
    seasons: [],
    getSeasonsByFiltered: vi.fn().mockResolvedValue({ items: [] }),
  } as unknown as ReturnType<typeof useSeason>);

  mockedGetChampionsHistory.mockResolvedValue({
    data: [],
  } as unknown as Awaited<ReturnType<typeof championService.getChampionsHistory>>);
});

const renderHome = () =>
  render(
    <MemoryRouter>
      <Home />
    </MemoryRouter>
  );

describe('Home — latest news card navigation', () => {
  it('navigates with the already-loaded post and does not pre-fetch it', async () => {
    const user = userEvent.setup();
    renderHome();

    const card = await screen.findByRole('button', { name: /Para cerrar el año/ });
    await user.click(card);

    await waitFor(() =>
      expect(navigateSpy).toHaveBeenCalledWith('/blog/para-cerrar-el-ano', {
        state: { post: expect.objectContaining({ slug: 'para-cerrar-el-ano' }) },
      })
    );
    // The detail page is the single owner of GET /api/blogposts/{slug} (the
    // only Views++ trigger). Pre-fetching here made the counter move twice.
    expect(getBlogPostsById).not.toHaveBeenCalled();
  });
});
