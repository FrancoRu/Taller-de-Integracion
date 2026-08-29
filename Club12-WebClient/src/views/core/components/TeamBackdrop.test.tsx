import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import TeamBackdrop from '@/views/core/components/TeamBackdrop';

describe('TeamBackdrop', () => {
  it('renders its children above the backdrop', () => {
    render(
      <TeamBackdrop shirtColor="#1e3a8a" logoUrl="https://example.com/logo.png">
        <span>contenido</span>
      </TeamBackdrop>
    );

    expect(screen.getByText('contenido')).toBeInTheDocument();
  });

  it('renders without a logo (tint-only background)', () => {
    render(
      <TeamBackdrop shirtColor="#0f766e" logoUrl={null}>
        <span>sin escudo</span>
      </TeamBackdrop>
    );

    expect(screen.getByText('sin escudo')).toBeInTheDocument();
  });
});
