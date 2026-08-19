import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import BlogPostPreviewDialog from '@/views/blogPost/BlogPostPreviewDialog';

describe('BlogPostPreviewDialog', () => {
  it('renders nothing when closed', () => {
    render(
      <BlogPostPreviewDialog
        open={false}
        onClose={vi.fn()}
        title="Gran final"
        author="Juan"
        markdownText="<p>Contenido</p>"
      />
    );

    expect(screen.queryByText('Gran final')).not.toBeInTheDocument();
  });

  it('renders the title, author, image and content when open', () => {
    render(
      <BlogPostPreviewDialog
        open
        onClose={vi.fn()}
        title="Gran final"
        author="Juan"
        photoUrl="blob:preview"
        markdownText="<p>Contenido de la nota</p>"
      />
    );

    expect(screen.getByText('Gran final')).toBeInTheDocument();
    expect(screen.getByText('Juan')).toBeInTheDocument();
    expect(screen.getByText('Contenido de la nota')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Gran final' })).toHaveAttribute(
      'src',
      'blob:preview'
    );
  });

  it('falls back to placeholder copy when title and author are empty', () => {
    render(
      <BlogPostPreviewDialog
        open
        onClose={vi.fn()}
        title=""
        author=""
        markdownText=""
      />
    );

    expect(screen.getByText('Sin título')).toBeInTheDocument();
    expect(screen.getByText('Autor sin definir')).toBeInTheDocument();
  });
});
