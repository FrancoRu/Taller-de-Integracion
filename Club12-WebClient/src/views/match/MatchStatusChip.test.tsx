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

  it('renders a distinct icon per status, not just the label text', () => {
    const { container: scheduled } = render(<MatchStatusChip status={MatchStatus.Scheduled} />);
    const { container: played } = render(<MatchStatusChip status={MatchStatus.Played} />);
    const { container: suspended } = render(<MatchStatusChip status={MatchStatus.Suspended} />);

    const iconTestId = (container: HTMLElement) =>
      container.querySelector('svg')?.getAttribute('data-testid');

    expect(iconTestId(scheduled)).toBe('ScheduleIcon');
    expect(iconTestId(played)).toBe('CheckCircleIcon');
    expect(iconTestId(suspended)).toBe('PauseCircleIcon');
    expect(iconTestId(scheduled)).not.toBe(iconTestId(played));
  });
});
