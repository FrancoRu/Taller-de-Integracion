import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { GUID } from '@/modules/core/types/types';
import { IPodium, IPodiumTeam } from '@/modules/champion/type/champion.d';
import Podium from '@/views/champion/Podium';

const guid = (value: string) => value as GUID;

const team = (id: string, name: string): IPodiumTeam => ({
  teamId: guid(id),
  teamName: name,
  logoUrl: null,
});

const podium = (overrides: Partial<IPodium> = {}): IPodium => ({
  divisionId: guid('division-1'),
  divisionName: 'Zona A',
  hasPlayoff: true,
  first: team('team-1', 'Los Halcones'),
  second: team('team-2', 'Los Pumas'),
  third: team('team-3', 'Los Cóndores'),
  ...overrides,
});

describe('Podium', () => {
  it('renders the three places (1º/2º/3º) with a decided podium', () => {
    render(<Podium podium={podium()} />);

    expect(screen.getByText('1º')).toBeInTheDocument();
    expect(screen.getByText('2º')).toBeInTheDocument();
    expect(screen.getByText('3º')).toBeInTheDocument();

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Los Pumas')).toBeInTheDocument();
    expect(screen.getByText('Los Cóndores')).toBeInTheDocument();
  });

  it('shows an "A definir" placeholder for undecided places', () => {
    render(
      <Podium podium={podium({ second: null, third: null })} />
    );

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getAllByText('A definir')).toHaveLength(2);
  });

  it('renders a top-three read straight from standings (no playoff)', () => {
    render(<Podium podium={podium({ hasPlayoff: false })} />);

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Los Pumas')).toBeInTheDocument();
    expect(screen.getByText('Los Cóndores')).toBeInTheDocument();
  });
});
