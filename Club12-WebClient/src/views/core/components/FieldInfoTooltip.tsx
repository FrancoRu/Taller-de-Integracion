import { IconButton, Tooltip } from '@mui/material';
import { InfoOutlinedIcon } from '@/views/core/MUI/icons/icons';

interface FieldInfoTooltipProps {
  /** Explanation shown on hover/focus — what the field is for, or what happens if it's left blank. */
  title: string;
}

/**
 * A small (i) icon that explains an optional field inline, meant to sit as a
 * TextField's `endAdornment`. Optional fields are marked by the ABSENCE of
 * the required-field `*` rather than a literal "(opcional)" suffix in the
 * label, so this is where that context lives instead.
 */
const FieldInfoTooltip: React.FC<FieldInfoTooltipProps> = ({ title }) => (
  <Tooltip title={title}>
    <IconButton size="small" edge="end" aria-label={title} sx={{ color: 'text.secondary' }}>
      <InfoOutlinedIcon fontSize="small" />
    </IconButton>
  </Tooltip>
);

export default FieldInfoTooltip;
