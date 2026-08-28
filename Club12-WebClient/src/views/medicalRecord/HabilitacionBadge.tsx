import { Chip, Tooltip } from '@mui/material';
import { MedicalRecordStatus } from '@/modules/core/enum/medicalRecord/medicalRecordStatus';
import {
  getHabilitacionColor,
  getHabilitacionLabel,
  getMedicalRecordDetail,
  resolveIsHabilitado,
} from '@/modules/medicalRecord/utils/medicalRecordDisplay';

interface HabilitacionBadgeProps {
  /** Explicit eligibility flag from the roster / record (HU-57). */
  isHabilitado?: boolean | null;
  /** Raw medical-record status, used for color/detail when not habilitado. */
  status?: MedicalRecordStatus | null;
  size?: 'small' | 'medium';
}

/**
 * "Habilitado / No habilitado" badge for a player, driven by the season
 * registration's eligibility (HU-57/HU-62). A not-habilitado player is shown
 * with a filled warning/error chip so it is visually clear on the roster and
 * where rosters are shown for match loading (HU-61).
 */
const HabilitacionBadge: React.FC<HabilitacionBadgeProps> = ({
  isHabilitado,
  status,
  size = 'small',
}) => {
  const habilitado = resolveIsHabilitado(isHabilitado, status);
  const label = getHabilitacionLabel(isHabilitado, status);
  const color = getHabilitacionColor(isHabilitado, status);
  const detail = getMedicalRecordDetail(status);

  return (
    <Tooltip title={detail}>
      <Chip
        size={size}
        label={label}
        color={color}
        variant={habilitado ? 'filled' : 'outlined'}
        aria-label={`${label}. ${detail}`}
      />
    </Tooltip>
  );
};

export default HabilitacionBadge;
