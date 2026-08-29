import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import PageShell from '@/views/core/components/PageShell';

describe('PageShell', () => {
  it('renders the title as the page heading and its children', () => {
    render(
      <PageShell title="Equipos">
        <p>contenido</p>
      </PageShell>
    );

    expect(
      screen.getByRole('heading', { level: 1, name: 'Equipos' })
    ).toBeInTheDocument();
    expect(screen.getByText('contenido')).toBeInTheDocument();
  });

  it('renders a back button with the given label and fires onClick', () => {
    const onClick = vi.fn();
    render(
      <PageShell back={{ label: 'Volver', onClick }}>
        <p>contenido</p>
      </PageShell>
    );

    const backButton = screen.getByRole('button', { name: /Volver/ });
    fireEvent.click(backButton);

    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('renders the actions region', () => {
    render(
      <PageShell title="Equipos" actions={<button>Nuevo</button>}>
        <p>contenido</p>
      </PageShell>
    );

    expect(screen.getByRole('button', { name: 'Nuevo' })).toBeInTheDocument();
  });

  it('does not render a header region when no title, actions or back are given', () => {
    const { container } = render(
      <PageShell>
        <p>contenido</p>
      </PageShell>
    );

    expect(container.querySelector('header')).toBeNull();
  });
});
