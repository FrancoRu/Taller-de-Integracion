import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import AddBlogPostForm from '@/views/blogPost/addBlogPostForm';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import * as confirmDialog from '@/modules/core/utils/confirmDialog';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('react-quill', () => ({
  default: ({
    value,
    onChange,
  }: {
    value: string;
    onChange: (content: string) => void;
  }) => (
    <textarea
      aria-label="Contenido"
      value={value}
      onChange={e => onChange(e.target.value)}
    />
  ),
}));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual =
    await vi.importActual<typeof import('react-router-dom')>(
      'react-router-dom'
    );
  return { ...actual, useNavigate: () => mockNavigate };
});

const mockedUseBlogPost = vi.mocked(useBlogPost);

const buildPost = (): BlogPostResponse => ({
  id: 'guid-a-aaaa-bbbb-cccc' as unknown as GUID,
  author: 'Autor',
  title: 'Titulo',
  views: 0,
  markdownText: 'contenido',
  createdAt: new Date(),
});

describe('AddBlogPostForm', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it('shows a success message and navigates home after a successful submit', async () => {
    const addBlogPost = vi.fn().mockResolvedValue(buildPost());
    mockedUseBlogPost.mockReturnValue({
      addBlogPost,
    } as unknown as ReturnType<typeof useBlogPost>);
    const notifySuccessSpy = vi
      .spyOn(confirmDialog, 'notifySuccess')
      .mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <AddBlogPostForm />
      </MemoryRouter>
    );

    fireEvent.change(screen.getByRole('textbox', { name: /autor/i }), {
      target: { value: 'Juan' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /título/i }), {
      target: { value: 'Gran final' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar' }));

    await waitFor(() => expect(addBlogPost).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(notifySuccessSpy).toHaveBeenCalled());
    expect(mockNavigate).toHaveBeenCalledWith('/');
  });

  it('does not navigate away when the submission fails', async () => {
    const addBlogPost = vi.fn().mockResolvedValue(undefined);
    mockedUseBlogPost.mockReturnValue({
      addBlogPost,
    } as unknown as ReturnType<typeof useBlogPost>);

    render(
      <MemoryRouter>
        <AddBlogPostForm />
      </MemoryRouter>
    );

    fireEvent.change(screen.getByRole('textbox', { name: /autor/i }), {
      target: { value: 'Juan' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /título/i }), {
      target: { value: 'Gran final' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar' }));

    await waitFor(() => expect(addBlogPost).toHaveBeenCalledTimes(1));
    expect(mockNavigate).not.toHaveBeenCalled();
  });
});
