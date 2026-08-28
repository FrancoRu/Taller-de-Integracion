import { describe, expect, it } from 'vitest';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import {
  getMatchStatusBadgeColor,
  getMatchStatusBadgeLabel,
  resolveMatchStatus,
} from '@/modules/match/utils/matchDisplay';

describe('matchDisplay status badge helpers', () => {
  it('maps each status to its Spanish label', () => {
    expect(getMatchStatusBadgeLabel(MatchStatus.Scheduled, false)).toBe(
      'Programado'
    );
    expect(getMatchStatusBadgeLabel(MatchStatus.Played, true)).toBe('Jugado');
    expect(getMatchStatusBadgeLabel(MatchStatus.Suspended, false)).toBe(
      'Suspendido'
    );
    expect(getMatchStatusBadgeLabel(MatchStatus.WalkOver, true)).toBe('W.O.');
  });

  it('maps each status to a distinct chip color', () => {
    expect(getMatchStatusBadgeColor(MatchStatus.Scheduled, false)).toBe(
      'default'
    );
    expect(getMatchStatusBadgeColor(MatchStatus.Played, true)).toBe('success');
    expect(getMatchStatusBadgeColor(MatchStatus.Suspended, false)).toBe(
      'warning'
    );
    expect(getMatchStatusBadgeColor(MatchStatus.WalkOver, true)).toBe('info');
  });

  it('falls back to isFinished when status is missing', () => {
    expect(resolveMatchStatus(null, true)).toBe(MatchStatus.Played);
    expect(resolveMatchStatus(undefined, false)).toBe(MatchStatus.Scheduled);
    // An explicit status always wins over the isFinished fallback.
    expect(resolveMatchStatus(MatchStatus.WalkOver, false)).toBe(
      MatchStatus.WalkOver
    );
  });
});
