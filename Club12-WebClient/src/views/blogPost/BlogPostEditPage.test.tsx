import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BlogPostEditPage from '@/views/blogPost/BlogPostEditPage';
import { useBlogPost } from '@/modules/blogPost/hook/blogPost.hook';
import * as confirmDialog from '@/modules/core/utils/confirmDialog';
import type { BlogPostResponse } from '@/modules/blogPost/type/blogPost';
import type { GUID } from '@/modules/core/types/types';

vi.mock('@/modules/blogPost/hook/blogPost.hook');
vi.mock('react-quill-new', () => ({
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
const POST_ID = 'guid-a-aaaa-bbbb-cccc';

const originalCreateObjectURL = URL.createObjectURL;
const originalRevokeObjectURL = URL.revokeObjectURL;

const buildPost = (
  overrides: Partial<BlogPostResponse> = {}
): BlogPostResponse => ({
  id: POST_ID as unknown as GUID,
  author: 'Autor',
  title: 'Titulo',
  slug: 'titulo',
  views: 0,
  markdownText: 'contenido',
  createdAt: new Date('2026-01-01'),
  isPublished: true,
  ...overrides,
});

const renderPage = () =>
  render(
    <MemoryRouter initialEntries={[`/panel/blog/${POST_ID}/editar`]}>
      <Routes>
        <Route
          path="/panel/blog/:blogPostId/editar"
          element={<BlogPostEditPage />}
        />
      </Routes>
    </MemoryRouter>
  );

describe('BlogPostEditPage', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    URL.createObjectURL = vi.fn().mockReturnValue('blob:preview-url');
    URL.revokeObjectURL = vi.fn();
  });

  afterEach(() => {
    URL.createObjectURL = originalCreateObjectURL;
    URL.revokeObjectURL = originalRevokeObjectURL;
  });

  it('shows the placeholder when the post has no photo yet', async () => {
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi.fn().mockResolvedValue(buildPost()),
      putBlogPostById: vi.fn(),
      putPhotoBlogPostById: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);

    renderPage();

    expect(
      await screen.findByRole('button', { name: 'Seleccionar imagen' })
    ).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
  });

  it('shows the current image preview when the post already has a photo', async () => {
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi
        .fn()
        .mockResolvedValue(buildPost({ photoUrl: 'https://cdn.example.com/a.jpg' })),
      putBlogPostById: vi.fn(),
      putPhotoBlogPostById: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);

    renderPage();

    const image = await screen.findByRole('img', {
      name: 'Imagen destacada de la publicación',
    });
    expect(image).toHaveAttribute('src', 'https://cdn.example.com/a.jpg');
    expect(
      screen.getByRole('button', { name: 'Cambiar imagen' })
    ).toBeInTheDocument();
  });

  it('uploads the new photo when saving after picking a file', async () => {
    const putBlogPostById = vi.fn().mockResolvedValue(buildPost());
    const putPhotoBlogPostById = vi.fn().mockResolvedValue(buildPost());
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi.fn().mockResolvedValue(buildPost()),
      putBlogPostById,
      putPhotoBlogPostById,
    } as unknown as ReturnType<typeof useBlogPost>);
    vi.spyOn(confirmDialog, 'notifySuccess').mockResolvedValue(undefined);

    renderPage();

    await screen.findByRole('button', { name: 'Seleccionar imagen' });

    const file = new File(['content'], 'foto.png', { type: 'image/png' });
    const input = document.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { files: [file] } });

    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() =>
      expect(putPhotoBlogPostById).toHaveBeenCalledWith(POST_ID, file)
    );
    expect(mockNavigate).toHaveBeenCalledWith('/panel/blog');
  });

  it('does not navigate away or show success when the photo upload fails', async () => {
    // putPhotoBlogPostById (blogPost.context.tsx) resolves to undefined on a
    // rejected upload (its own catch calls handleUnknownError and returns
    // nothing) — the page must treat that as a real failure, not silently
    // proceed as if the whole save succeeded.
    const putBlogPostById = vi.fn().mockResolvedValue(buildPost());
    const putPhotoBlogPostById = vi.fn().mockResolvedValue(undefined);
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi.fn().mockResolvedValue(buildPost()),
      putBlogPostById,
      putPhotoBlogPostById,
    } as unknown as ReturnType<typeof useBlogPost>);
    // vi.spyOn on an already-spied module method returns the SAME mock and
    // keeps its prior call history — this file has no clearMocks/beforeEach
    // reset, so an earlier test's successful notifySuccess call would
    // otherwise still be sitting in this spy's call log.
    const notifySuccessSpy = vi
      .spyOn(confirmDialog, 'notifySuccess')
      .mockResolvedValue(undefined);
    notifySuccessSpy.mockClear();

    renderPage();

    await screen.findByRole('button', { name: 'Seleccionar imagen' });

    const file = new File(['content'], 'foto.png', { type: 'image/png' });
    const input = document.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { files: [file] } });

    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    // Wait for the button to re-enable (setSubmitting(false), the last thing
    // the early-return failure path does) rather than just "the mock was
    // called" — that fires a tick before the rest of the same async
    // function (the `if (!withPhoto) return;` check) has actually run.
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Guardar' })).toBeEnabled()
    );
    expect(putPhotoBlogPostById).toHaveBeenCalledWith(POST_ID, file);
    expect(notifySuccessSpy).not.toHaveBeenCalled();
    expect(mockNavigate).not.toHaveBeenCalledWith('/panel/blog');
  });

  it('does not upload a photo when saving without picking a new file', async () => {
    const putBlogPostById = vi.fn().mockResolvedValue(buildPost());
    const putPhotoBlogPostById = vi.fn();
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi.fn().mockResolvedValue(buildPost()),
      putBlogPostById,
      putPhotoBlogPostById,
    } as unknown as ReturnType<typeof useBlogPost>);
    vi.spyOn(confirmDialog, 'notifySuccess').mockResolvedValue(undefined);

    renderPage();

    await screen.findByRole('button', { name: 'Seleccionar imagen' });
    fireEvent.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() => expect(putBlogPostById).toHaveBeenCalledTimes(1));
    expect(putPhotoBlogPostById).not.toHaveBeenCalled();
  });

  it('opens a preview of the current draft', async () => {
    mockedUseBlogPost.mockReturnValue({
      getBlogPostsById: vi.fn().mockResolvedValue(buildPost({ title: 'Gran final' })),
      putBlogPostById: vi.fn(),
      putPhotoBlogPostById: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);

    renderPage();

    await screen.findByRole('button', { name: 'Seleccionar imagen' });
    fireEvent.click(screen.getByRole('button', { name: 'Vista previa' }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    expect(screen.getAllByText('Gran final').length).toBeGreaterThan(0);
  });
});
