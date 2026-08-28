import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import AddBlogPostForm from '@/views/blogPost/addBlogPostForm';
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

const originalCreateObjectURL = URL.createObjectURL;
const originalRevokeObjectURL = URL.revokeObjectURL;

const buildPost = (): BlogPostResponse => ({
  id: 'guid-a-aaaa-bbbb-cccc' as unknown as GUID,
  author: 'Autor',
  title: 'Titulo',
  slug: 'titulo',
  views: 0,
  markdownText: 'contenido',
  createdAt: new Date(),
  isPublished: true,
});

describe('AddBlogPostForm', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
    URL.createObjectURL = vi.fn().mockReturnValue('blob:preview-url');
    URL.revokeObjectURL = vi.fn();
  });

  afterEach(() => {
    URL.createObjectURL = originalCreateObjectURL;
    URL.revokeObjectURL = originalRevokeObjectURL;
  });

  it('shows a success message and navigates to the admin blog list after a successful submit', async () => {
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
    expect(mockNavigate).toHaveBeenCalledWith('/panel/blog');
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

  it('includes the selected photo file in the create request', async () => {
    const addBlogPost = vi.fn().mockResolvedValue(buildPost());
    mockedUseBlogPost.mockReturnValue({
      addBlogPost,
    } as unknown as ReturnType<typeof useBlogPost>);
    vi.spyOn(confirmDialog, 'notifySuccess').mockResolvedValue(undefined);

    render(
      <MemoryRouter>
        <AddBlogPostForm />
      </MemoryRouter>
    );

    const file = new File(['content'], 'foto.png', { type: 'image/png' });
    const input = document.querySelector(
      'input[type="file"]'
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { files: [file] } });

    expect(
      screen.getByRole('button', { name: 'Cambiar imagen' })
    ).toBeInTheDocument();

    fireEvent.change(screen.getByRole('textbox', { name: /autor/i }), {
      target: { value: 'Juan' },
    });
    fireEvent.change(screen.getByRole('textbox', { name: /título/i }), {
      target: { value: 'Gran final' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Publicar' }));

    await waitFor(() =>
      expect(addBlogPost).toHaveBeenCalledWith(
        expect.objectContaining({ photoFile: file })
      )
    );
  });

  it('sends isPublished true by default (published) on submit (HU-16)', async () => {
    const addBlogPost = vi.fn().mockResolvedValue(buildPost());
    mockedUseBlogPost.mockReturnValue({
      addBlogPost,
    } as unknown as ReturnType<typeof useBlogPost>);
    vi.spyOn(confirmDialog, 'notifySuccess').mockResolvedValue(undefined);

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

    await waitFor(() =>
      expect(addBlogPost).toHaveBeenCalledWith(
        expect.objectContaining({ isPublished: true })
      )
    );
  });

  it('sends isPublished false when saved as a draft (HU-16)', async () => {
    const addBlogPost = vi.fn().mockResolvedValue(buildPost());
    mockedUseBlogPost.mockReturnValue({
      addBlogPost,
    } as unknown as ReturnType<typeof useBlogPost>);
    vi.spyOn(confirmDialog, 'notifySuccess').mockResolvedValue(undefined);

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
    // Toggle the draft/published switch off -> draft.
    fireEvent.click(screen.getByRole('switch'));
    fireEvent.click(screen.getByRole('button', { name: 'Guardar borrador' }));

    await waitFor(() =>
      expect(addBlogPost).toHaveBeenCalledWith(
        expect.objectContaining({ isPublished: false })
      )
    );
  });

  it('opens a preview with the current form content without submitting', async () => {
    mockedUseBlogPost.mockReturnValue({
      addBlogPost: vi.fn(),
    } as unknown as ReturnType<typeof useBlogPost>);

    render(
      <MemoryRouter>
        <AddBlogPostForm />
      </MemoryRouter>
    );

    fireEvent.change(screen.getByRole('textbox', { name: /título/i }), {
      target: { value: 'Gran final' },
    });
    fireEvent.click(screen.getByRole('button', { name: 'Vista previa' }));

    expect(await screen.findByRole('dialog')).toBeInTheDocument();
    expect(screen.getAllByText('Gran final').length).toBeGreaterThan(0);
  });
});
