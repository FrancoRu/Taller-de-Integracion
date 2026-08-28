import { Chip } from '@mui/material';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import {
  getMatchStatusBadgeColor,
  getMatchStatusBadgeLabel,
} from '@/modules/match/utils/matchDisplay';

interface MatchStatusChipProps {
  status: MatchStatus | null | undefined;
  /**
   * Legacy fallback: when `status` is missing, the badge is derived from
   * whether the match is finished.
   */
  isFinished?: boolean;
  size?: 'small' | 'medium';
}

/**
 * A status badge for a match (HU-69/HU-73). Renders Programado / Jugado /
 * Suspendido / W.O. so a walkover is visually distinct from a normal result.
 */
export default function MatchStatusChip({
  status,
  isFinished = false,
  size = 'small',
}: MatchStatusChipProps) {
  return (
    <Chip
      label={getMatchStatusBadgeLabel(status, isFinished)}
      color={getMatchStatusBadgeColor(status, isFinished)}
      size={size}
    />
  );
}
