import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import TeamHero from '@/views/core/components/TeamHero';

describe('TeamHero', () => {
  it('renders the team name as the page heading and the code', () => {
    render(<TeamHero name="River Plate" code="RIV" shirtColor="#e11d2a" />);

    expect(
      screen.getByRole('heading', { level: 1, name: 'River Plate' })
    ).toBeInTheDocument();
    expect(screen.getByText('RIV')).toBeInTheDocument();
  });

  it('renders the team jersey with an accessible label', () => {
    render(<TeamHero name="Boca" shirtColor="#1e3a8a" jerseyStyle="stripes" />);

    expect(
      screen.getByRole('img', { name: 'Camiseta de Boca' })
    ).toBeInTheDocument();
  });

  it('renders without a logo (graceful tint-only background)', () => {
    render(<TeamHero name="Sin Escudo" shirtColor="#0f766e" logoUrl={null} />);

    expect(
      screen.getByRole('heading', { level: 1, name: 'Sin Escudo' })
    ).toBeInTheDocument();
  });

  it('renders children below the identity row', () => {
    render(
      <TeamHero name="River" shirtColor="#e11d2a">
        <span>plantel</span>
      </TeamHero>
    );

    expect(screen.getByText('plantel')).toBeInTheDocument();
  });
});
