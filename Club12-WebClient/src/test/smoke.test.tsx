import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import LoadingIndicator from '@/views/core/components/LoadingIndicator';

describe('LoadingIndicator', () => {
  it('renders the loading text', () => {
    render(<LoadingIndicator />);

    expect(screen.getByText('Cargando...')).toBeInTheDocument();
  });
});
