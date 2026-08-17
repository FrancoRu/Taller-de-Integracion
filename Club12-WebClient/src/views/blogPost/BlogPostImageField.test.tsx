import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import BlogPostImageField from '@/views/blogPost/BlogPostImageField';

describe('BlogPostImageField', () => {
  it('shows the placeholder icon and "Seleccionar imagen" when there is no image yet', () => {
    render(<BlogPostImageField hasImage={false} onFileSelect={vi.fn()} />);

    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: 'Seleccionar imagen' })
    ).toBeInTheDocument();
  });

  it('shows the preview image and "Cambiar imagen" when a preview URL is provided', () => {
    render(
      <BlogPostImageField
        previewUrl="https://cdn.example.com/photo.jpg"
        hasImage
        onFileSelect={vi.fn()}
      />
    );

    const image = screen.getByRole('img', {
      name: 'Imagen destacada de la publicación',
    });
    expect(image).toHaveAttribute('src', 'https://cdn.example.com/photo.jpg');
    expect(
      screen.getByRole('button', { name: 'Cambiar imagen' })
    ).toBeInTheDocument();
  });

  it('calls onFileSelect with the chosen file', () => {
    const onFileSelect = vi.fn();
    render(<BlogPostImageField hasImage={false} onFileSelect={onFileSelect} />);

    const file = new File(['content'], 'foto.png', { type: 'image/png' });
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;

    fireEvent.change(input, { target: { files: [file] } });

    expect(onFileSelect).toHaveBeenCalledWith(file);
  });
});
