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

  it('shows an "A definir" placeholder for undecided standings places', () => {
    render(
      <Podium podium={podium({ hasPlayoff: false, second: null, third: null })} />
    );

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getAllByText('A definir')).toHaveLength(2);
  });

  it('omits the 3rd place in a playoff with no third-place match', () => {
    render(<Podium podium={podium({ third: null })} />);

    expect(screen.getByText('1º')).toBeInTheDocument();
    expect(screen.getByText('2º')).toBeInTheDocument();
    expect(screen.queryByText('3º')).not.toBeInTheDocument();
    expect(screen.queryByText('A definir')).not.toBeInTheDocument();
  });

  it('holds the 3rd column open with a spacer when there is no 3rd place, instead of omitting it', () => {
    const { container } = render(<Podium podium={podium({ third: null })} />);

    // DOM order: champion first (screen readers announce it first), then
    // runner-up — visual position (middle column vs left) comes from CSS
    // `order`, which jsdom can't meaningfully resolve in a unit test; that
    // part is verified visually against staging instead.
    const champion = screen.getByText('Los Halcones');
    const runnerUp = screen.getByText('Los Pumas');
    expect(
      champion.compareDocumentPosition(runnerUp) & Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();

    expect(container.querySelectorAll('.MuiPaper-root')).toHaveLength(2);
    // Both places here are decided (real teams, no "A definir" fallback
    // circle), so the only aria-hidden element left is the 3rd-column
    // spacer that keeps the champion's column the row's true center
    // instead of the pair centering as a unit with the champion off to
    // one side.
    expect(container.querySelector('[aria-hidden="true"]')).not.toBeNull();
  });

  it('renders a top-three read straight from standings (no playoff)', () => {
    render(<Podium podium={podium({ hasPlayoff: false })} />);

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Los Pumas')).toBeInTheDocument();
    expect(screen.getByText('Los Cóndores')).toBeInTheDocument();
  });
});
