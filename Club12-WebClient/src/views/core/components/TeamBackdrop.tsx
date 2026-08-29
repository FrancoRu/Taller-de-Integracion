import type { ReactNode } from 'react';
import { Box } from '@mui/material';
import { hexToRgba, resolveShirtColor } from '@/design/colorName';
import { pageMinHeight } from '@/design/tokens';

export interface TeamBackdropProps {
  /** Primary shirt color as a `#rrggbb` hex; tints the page background. */
  shirtColor?: string | null;
  /** Team escudo; rendered as a faint diagonal watermark, when present. */
  logoUrl?: string | null;
  children: ReactNode;
}

/**
 * A full-bleed page background that takes on a team's identity: a soft glow of
 * the team's shirt color rising from the bottom-right corner, with the club
 * escudo bleeding in diagonally from that same corner as an oversized, faint,
 * tilted watermark faded toward the top-left. The content is layered above and
 * stays fully legible. Reusable across any team-scoped public page so the
 * treatment is defined once instead of hardcoded per view.
 */
export default function TeamBackdrop({
  shirtColor,
  logoUrl,
  children,
}: TeamBackdropProps) {
  const { fill } = resolveShirtColor(shirtColor);
  const glow = `radial-gradient(120% 120% at 100% 100%, ${hexToRgba(
    fill,
    0.22
  )} 0%, ${hexToRgba(fill, 0.06)} 42%, transparent 72%)`;

  // Fade every decorative layer out over the bottom of the page so the tint and
  // watermark dissolve into the footer instead of ending in a hard horizontal
  // edge. Only the decoration is masked — the content sits outside it.
  const bottomFade = 'linear-gradient(to top, transparent 0%, #000 16%, #000 100%)';

  return (
    <Box sx={{ position: 'relative', overflow: 'hidden', minHeight: pageMinHeight }}>
      <Box
        aria-hidden="true"
        sx={{
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
          maskImage: bottomFade,
          WebkitMaskImage: bottomFade,
        }}
      >
        {/* Team-color glow rising from the bottom-right corner. */}
        <Box sx={{ position: 'absolute', inset: 0, background: glow }} />
        {/* Escudo watermark bleeding in diagonally from the bottom-right corner. */}
        {logoUrl && (
          <Box
            sx={{
              position: 'absolute',
              right: { xs: -48, sm: -56 },
              bottom: { xs: 40, sm: 120 },
              width: { xs: 260, sm: 440 },
              height: { xs: 260, sm: 440 },
              backgroundImage: `url(${logoUrl})`,
              backgroundSize: 'contain',
              backgroundPosition: 'bottom right',
              backgroundRepeat: 'no-repeat',
              transform: 'rotate(-30deg)',
              transformOrigin: 'bottom right',
              opacity: 0.12,
              // Solid at the bottom-right, dissolving toward the top-left so the
              // watermark melts into the page instead of ending in a hard edge.
              maskImage:
                'linear-gradient(to top left, #000 0%, #000 40%, transparent 80%)',
              WebkitMaskImage:
                'linear-gradient(to top left, #000 0%, #000 40%, transparent 80%)',
            }}
          />
        )}
      </Box>
      {/* Foreground content. */}
      <Box sx={{ position: 'relative' }}>{children}</Box>
    </Box>
  );
}
