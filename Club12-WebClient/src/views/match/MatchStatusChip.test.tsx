import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import MatchStatusChip from '@/views/match/MatchStatusChip';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';

describe('MatchStatusChip', () => {
  it('renders the W.O. badge for a walkover', () => {
    render(<MatchStatusChip status={MatchStatus.WalkOver} />);
    expect(screen.getByText('W.O.')).toBeInTheDocument();
  });

  it('renders the Jugado badge for a played match', () => {
    render(<MatchStatusChip status={MatchStatus.Played} />);
    expect(screen.getByText('Jugado')).toBeInTheDocument();
  });

  it('falls back to isFinished when status is missing', () => {
    render(<MatchStatusChip status={null} isFinished />);
    expect(screen.getByText('Jugado')).toBeInTheDocument();
  });
});
