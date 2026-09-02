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

  it('renders the champion above the runner-up when there is no 3rd place — not the other way around', () => {
    const { container } = render(<Podium podium={podium({ third: null })} />);

    const champion = screen.getByText('Los Halcones');
    const runnerUp = screen.getByText('Los Pumas');
    // DOM order alone isn't enough proof — a leftover flex `order` style can
    // still visually reorder same-parent siblings regardless of DOM order —
    // so this also asserts the champion's box has no `order` overriding it
    // past the runner-up line (see PodiumPlace's `standalone` prop).
    expect(
      champion.compareDocumentPosition(runnerUp) & Node.DOCUMENT_POSITION_FOLLOWING
    ).toBeTruthy();

    const championBox = container.querySelector('.MuiPaper-root');
    expect(championBox).not.toBeNull();
    expect(getComputedStyle(championBox!).order).not.toBe('2');
    expect(getComputedStyle(championBox!).order).not.toBe('1');
  });

  it('renders a top-three read straight from standings (no playoff)', () => {
    render(<Podium podium={podium({ hasPlayoff: false })} />);

    expect(screen.getByText('Los Halcones')).toBeInTheDocument();
    expect(screen.getByText('Los Pumas')).toBeInTheDocument();
    expect(screen.getByText('Los Cóndores')).toBeInTheDocument();
  });
});
