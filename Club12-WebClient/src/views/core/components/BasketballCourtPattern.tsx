import { Box, SxProps, Theme } from '@mui/material';

interface BasketballCourtPatternProps {
  sx?: SxProps<Theme>;
}

/**
 * Decorative full-court basketball diagram (boundary, center circle, both
 * keys with rebound hash marks, both free-throw circles, both hoops with a
 * restricted-area arc, both three-point arcs) drawn as inline SVG so it
 * needs no external image asset. Meant as a low-opacity background layer,
 * not a literal court — stroke-only (plus two small solid hoop dots), scales
 * to its container.
 */
export default function BasketballCourtPattern({ sx }: BasketballCourtPatternProps) {
  return (
    <Box
      component="svg"
      viewBox="0 0 940 500"
      preserveAspectRatio="xMidYMid slice"
      aria-hidden="true"
      sx={{
        position: 'absolute',
        inset: 0,
        width: '100%',
        height: '100%',
        stroke: 'currentColor',
        fill: 'none',
        strokeWidth: 2.5,
        ...sx,
      }}
    >
      <rect x={10} y={10} width={920} height={480} rx={4} />
      <line x1={470} y1={10} x2={470} y2={490} />
      <circle cx={470} cy={250} r={70} />
      <circle cx={470} cy={250} r={8} fill="currentColor" stroke="none" />

      {/* Left key, hoop and three-point arc. The arc is a true circle
          CENTERED ON THE HOOP (60, 250), radius 220, clipped to where it
          crosses the baseline (x=10) — the only construction that actually
          keeps every point of the line equidistant from the hoop, the way a
          real three-point line works. Picking an arbitrary radius/endpoints
          without anchoring to the hoop is what produced a flattened, wrong
          curve before. */}
      <rect x={10} y={160} width={190} height={180} />
      <circle cx={200} cy={250} r={70} />
      <line x1={10} y1={190} x2={22} y2={190} />
      <line x1={10} y1={220} x2={22} y2={220} />
      <line x1={10} y1={280} x2={22} y2={280} />
      <line x1={10} y1={310} x2={22} y2={310} />
      <path d="M 60 220 A 30 30 0 0 1 60 280" />
      <circle cx={60} cy={250} r={4} fill="currentColor" stroke="none" />
      <path d="M 10 35.76 A 220 220 0 1 1 10 464.24" />

      {/* Right key, hoop and three-point arc — mirrored about x=470. */}
      <rect x={740} y={160} width={190} height={180} />
      <circle cx={740} cy={250} r={70} />
      <line x1={930} y1={190} x2={918} y2={190} />
      <line x1={930} y1={220} x2={918} y2={220} />
      <line x1={930} y1={280} x2={918} y2={280} />
      <line x1={930} y1={310} x2={918} y2={310} />
      <path d="M 880 220 A 30 30 0 0 0 880 280" />
      <circle cx={880} cy={250} r={4} fill="currentColor" stroke="none" />
      <path d="M 930 35.76 A 220 220 0 1 0 930 464.24" />
    </Box>
  );
}
