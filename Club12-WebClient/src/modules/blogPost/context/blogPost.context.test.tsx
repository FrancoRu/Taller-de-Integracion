import { act, renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';
import Swal from 'sweetalert2';
import { ErrorProvider } from '@/modules/error/context/error.context';
import { BlogPostProvider } from '@/modules/blogPost/context/blogPost.context';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import { blogPostService } from '@/modules/blogPost/service/blogPost.service';

vi.mock('@/modules/blogPost/service/blogPost.service');
vi.mock('sweetalert2', () => ({
  default: {
    fire: vi.fn(),
    getContainer: vi.fn().mockReturnValue(null),
  },
}));

const mockedAddBlogPost = vi.mocked(blogPostService.addBlogPost);
const mockedSwalFire = vi.mocked(Swal.fire);

const wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={new QueryClient()}>
    <ErrorProvider>
      <BlogPostProvider>{children}</BlogPostProvider>
    </ErrorProvider>
  </QueryClientProvider>
);

beforeEach(() => {
  vi.clearAllMocks();
});

describe('BlogPostProvider — no duplicate success toast', () => {
  /**
   * addBlogPostForm.tsx already shows its own confirmation. The context used
   * to ALSO fire a toast ("Blog Post created successfully" — also the only
   * English string in these flows), so the user saw two modals for one save.
   */
  it('does not fire its own toast after addBlogPost succeeds', async () => {
    mockedAddBlogPost.mockResolvedValueOnce({
      status: 201,
      data: { id: '77777777-7777-7777-7777-777777777777' },
    } as never);

    const { result } = renderHook(() => useBlogPost(), { wrapper });
    await act(async () => {
      await result.current.addBlogPost({} as never);
    });

    expect(mockedSwalFire).not.toHaveBeenCalled();
  });
});
