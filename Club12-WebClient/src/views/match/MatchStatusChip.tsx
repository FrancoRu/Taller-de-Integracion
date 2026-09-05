import { ReactElement } from 'react';
import { Chip } from '@mui/material';
import { MatchStatus } from '@/modules/core/enum/match/matchStatus';
import {
  getMatchStatusBadgeColor,
  getMatchStatusBadgeLabel,
  resolveMatchStatus,
} from '@/modules/match/utils/matchDisplay';
import {
  CancelIcon,
  CheckCircleIcon,
  EventBusyIcon,
  PauseCircleIcon,
  ScheduleIcon,
} from '@/views/core/MUI/icons/icons';

interface MatchStatusChipProps {
  status: MatchStatus | null | undefined;
  /**
   * Legacy fallback: when `status` is missing, the badge is derived from
   * whether the match is finished.
   */
  isFinished?: boolean;
  size?: 'small' | 'medium';
}

/** One glyph per status, so the badge reads at a glance without parsing the label text. */
const STATUS_ICON: Record<MatchStatus, ReactElement> = {
  [MatchStatus.Scheduled]: <ScheduleIcon />,
  [MatchStatus.Played]: <CheckCircleIcon />,
  [MatchStatus.Suspended]: <PauseCircleIcon />,
  [MatchStatus.WalkOver]: <EventBusyIcon />,
  [MatchStatus.Canceled]: <CancelIcon />,
};

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
      icon={STATUS_ICON[resolveMatchStatus(status, isFinished)]}
      label={getMatchStatusBadgeLabel(status, isFinished)}
      color={getMatchStatusBadgeColor(status, isFinished)}
      size={size}
    />
  );
}
