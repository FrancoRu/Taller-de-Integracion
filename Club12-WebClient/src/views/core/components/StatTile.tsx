import { ReactNode } from 'react';
import { Box, Typography } from '@mui/material';
import { font } from '@/design/tokens';

/** The emphasis tone of a tile's value, mapped to a theme color. */
export type StatTileTone = 'neutral' | 'positive' | 'negative' | 'accent';

const TONE_COLOR: Record<StatTileTone, string> = {
  neutral: 'text.primary',
  positive: 'success.main',
  negative: 'error.main',
  accent: 'primary.main',
};

export interface StatTileProps {
  /** Short uppercase caption above the value (e.g. "Posición"). */
  label: string;
  /** The headline value — big condensed numerals (e.g. "3º", "5-2", "+12"). */
  value: ReactNode;
  /** Optional secondary line under the value (e.g. "de 8", division name). */
  sub?: ReactNode;
  /** Colors the value; defaults to `neutral`. */
  tone?: StatTileTone;
}

/**
 * The box-score signature of the team profile: a single scoreboard stat, read
 * as a dark chip with a small caption, one big Oswald numeral value and an
 * optional sub-line. Tiles line up in a wrapping row to read like a scoreboard
 * strip; the value tone carries meaning (a green/red differential, an accent
 * highlight) without adding chrome.
 */
export default function StatTile({
  label,
  value,
  sub,
  tone = 'neutral',
}: StatTileProps) {
  return (
    <Box
      sx={{
        flex: '1 1 96px',
        minWidth: 96,
        px: 2,
        py: 1.5,
        borderRadius: 2,
        bgcolor: 'action.hover',
        border: '1px solid',
        borderColor: 'divider',
        textAlign: 'center',
      }}
    >
      <Typography
        variant="overline"
        component="p"
        sx={{
          color: 'text.secondary',
          lineHeight: 1.4,
          display: 'block',
          letterSpacing: '0.08em',
        }}
      >
        {label}
      </Typography>
      <Typography
        component="p"
        sx={{
          fontFamily: font.display,
          fontWeight: 700,
          fontSize: { xs: '1.6rem', sm: '1.9rem' },
          lineHeight: 1.05,
          color: TONE_COLOR[tone],
          fontVariantNumeric: 'tabular-nums',
        }}
      >
        {value}
      </Typography>
      {sub && (
        <Typography
          variant="caption"
          component="p"
          noWrap
          sx={{ color: 'text.secondary', display: 'block', mt: 0.25 }}
        >
          {sub}
        </Typography>
      )}
    </Box>
  );
}
