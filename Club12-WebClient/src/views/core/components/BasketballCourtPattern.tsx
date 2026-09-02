import { Box, SxProps, Theme } from '@mui/material';

interface BasketballCourtPatternProps {
  sx?: SxProps<Theme>;
}

/**
 * Decorative full FIBA court diagram (28m × 15m, real proportions — key,
 * free-throw circle and semicircle, restricted-area arc, three-point line
 * with its straight corner sections and the arc centered on the hoop, and
 * rebound hash marks) drawn as inline SVG so it needs no external image
 * asset. Meant as a low-opacity background layer, not a literal court —
 * stroke-only (plus two small solid hoop dots), scales to its container.
 */
export default function BasketballCourtPattern({ sx }: BasketballCourtPatternProps) {
  return (
    <Box
      component="svg"
      // Padded beyond the court's own 28x15 so "slice" doesn't scale it up to
      // fill the container edge-to-edge (which crops it down to an
      // unrecognizable close-up of just the paint/hoop area) — but only a
      // little: enough padding to read as a court at a glance, not so much
      // that the lines shrink into an unremarkable texture.
      viewBox="-2 -1 32 17"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      sx={{
        position: 'absolute',
        inset: 0,
        width: '100%',
        height: '100%',
        stroke: 'currentColor',
        fill: 'none',
        strokeWidth: 0.055,
        strokeLinecap: 'round',
        strokeLinejoin: 'round',
        opacity: 0.12,
        ...sx,
      }}
    >
      {/* Court boundary */}
      <rect x={0} y={0} width={28} height={15} />
      {/* Center line */}
      <line x1={14} y1={0} x2={14} y2={15} />
      {/* Center circle — diameter 3.60m */}
      <circle cx={14} cy={7.5} r={1.8} />

      {/* ---- Left basket ---- */}
      {/* Basket center, 1.575m from the endline */}
      <circle cx={1.575} cy={7.5} r={0.225} />
      <circle cx={1.575} cy={7.5} r={0.07} fill="currentColor" stroke="none" />
      <line x1={1.2} y1={6.9} x2={1.2} y2={8.1} />
      <line x1={1.2} y1={7.5} x2={1.575} y2={7.5} />

      {/* ---- Left key ---- */}
      <rect x={0} y={4.55} width={5.8} height={5.9} />
      <line x1={5.8} y1={4.55} x2={5.8} y2={10.45} />
      {/* Free-throw circle — diameter 3.60m */}
      <circle cx={5.8} cy={7.5} r={1.8} />
      <path d="M 5.8 5.7 A 1.8 1.8 0 0 1 5.8 9.3" />

      {/* Restricted (no-charge) arc — radius 1.25m, centered on the hoop */}
      <path d="M 1.575 6.25 A 1.25 1.25 0 0 1 1.575 8.75" />

      {/* Three-point line: straight sections 0.90m from each sideline, then
          an arc of radius 6.75m centered on the hoop (1.575, 7.5) — the
          straight/arc junction (2.989, 0.9) is where that circle crosses
          y=0.9: x = 1.575 + sqrt(6.75² - 6.6²). */}
      <line x1={0} y1={0.9} x2={2.989} y2={0.9} />
      <path d="M 2.989 0.9 A 6.75 6.75 0 0 1 2.989 14.1" />
      <line x1={2.989} y1={14.1} x2={0} y2={14.1} />

      {/* Rebound hash marks along the key */}
      <line x1={4.15} y1={4.55} x2={4.15} y2={4.9} />
      <line x1={4.95} y1={4.55} x2={4.95} y2={4.9} />
      <line x1={4.15} y1={10.1} x2={4.15} y2={10.45} />
      <line x1={4.95} y1={10.1} x2={4.95} y2={10.45} />

      {/* ---- Right basket — mirrored ---- */}
      <circle cx={26.425} cy={7.5} r={0.225} />
      <circle cx={26.425} cy={7.5} r={0.07} fill="currentColor" stroke="none" />
      <line x1={26.8} y1={6.9} x2={26.8} y2={8.1} />
      <line x1={26.8} y1={7.5} x2={26.425} y2={7.5} />

      {/* ---- Right key ---- */}
      <rect x={22.2} y={4.55} width={5.8} height={5.9} />
      <line x1={22.2} y1={4.55} x2={22.2} y2={10.45} />
      <circle cx={22.2} cy={7.5} r={1.8} />
      <path d="M 22.2 5.7 A 1.8 1.8 0 0 0 22.2 9.3" />

      <path d="M 26.425 6.25 A 1.25 1.25 0 0 0 26.425 8.75" />

      <line x1={28} y1={0.9} x2={25.011} y2={0.9} />
      <path d="M 25.011 0.9 A 6.75 6.75 0 0 0 25.011 14.1" />
      <line x1={25.011} y1={14.1} x2={28} y2={14.1} />

      <line x1={23.85} y1={4.55} x2={23.85} y2={4.9} />
      <line x1={23.05} y1={4.55} x2={23.05} y2={4.9} />
      <line x1={23.85} y1={10.1} x2={23.85} y2={10.45} />
      <line x1={23.05} y1={10.1} x2={23.05} y2={10.45} />
    </Box>
  );
}
