import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import ErrorBoundary from './error-boundary';

function Boom(): never {
  throw new Error('boom');
}

describe('ErrorBoundary', () => {
  it('renders the fallback without a Router in the tree', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>
    );

    expect(screen.getByText('Algo salió mal')).toBeInTheDocument();

    consoleError.mockRestore();
  });

  it('offers a plain link back to the home route, not a react-router Link', () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});

    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>
    );

    const backButton = screen.getByRole('link', { name: 'Volver al inicio' });
    expect(backButton).toHaveAttribute('href', '/');

    consoleError.mockRestore();
  });

  it('renders children normally when nothing throws', () => {
    render(
      <ErrorBoundary>
        <div>all good</div>
      </ErrorBoundary>
    );

    expect(screen.getByText('all good')).toBeInTheDocument();
  });
});
