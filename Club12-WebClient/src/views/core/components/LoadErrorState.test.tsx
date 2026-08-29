import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import LoadErrorState from '@/views/core/components/LoadErrorState';

describe('LoadErrorState', () => {
  it('renders a default load-failure message', () => {
    render(<LoadErrorState onRetry={vi.fn()} />);

    expect(screen.getByText(/No pudimos cargar/i)).toBeInTheDocument();
  });

  it('renders a custom message when provided', () => {
    render(
      <LoadErrorState message="No pudimos cargar las temporadas." onRetry={vi.fn()} />
    );

    expect(
      screen.getByText('No pudimos cargar las temporadas.')
    ).toBeInTheDocument();
  });

  it('exposes a real "Reintentar" button and calls onRetry when clicked', () => {
    const onRetry = vi.fn();
    render(<LoadErrorState onRetry={onRetry} />);

    const button = screen.getByRole('button', { name: 'Reintentar' });
    fireEvent.click(button);

    expect(onRetry).toHaveBeenCalledTimes(1);
  });
});
