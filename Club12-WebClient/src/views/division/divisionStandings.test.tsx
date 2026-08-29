import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import DivisionStandings from '@/views/division/divisionStandings';
import { Position } from '@/modules/division/type/division';
import type { GUID } from '@/modules/core/types/types';

const buildPosition = (overrides: Partial<Position>): Position => ({
  teamId: 'team-1' as unknown as GUID,
  teamName: 'Aguará',
  logoUrl: 'http://example.com/logo.png',
  matchesPlayed: 3,
  wins: 2,
  losses: 1,
  pointsFor: 200,
  pointsAgainst: 180,
  pointsDifference: 20,
  points: 5,
  ...overrides,
});

describe('DivisionStandings point deduction note', () => {
  it('shows a subtle "-N" note with the reason when a team has a deduction', () => {
    const positions: Position[] = [
      buildPosition({
        teamId: 'team-1' as unknown as GUID,
        teamName: 'Aguará',
        points: 4,
        pointDeduction: { points: 2, reason: 'Alineación indebida' },
      }),
    ];

    render(<DivisionStandings positions={positions} />);

    expect(screen.getByText('-2')).toBeInTheDocument();
    expect(
      screen.getByLabelText('Deducción de 2 puntos: Alineación indebida')
    ).toBeInTheDocument();
  });

  it('renders no deduction note when the team has none', () => {
    const positions: Position[] = [
      buildPosition({ teamName: 'Sin sanción', points: 6 }),
    ];

    render(<DivisionStandings positions={positions} />);

    expect(screen.queryByText(/^-\d+$/)).not.toBeInTheDocument();
  });
});
