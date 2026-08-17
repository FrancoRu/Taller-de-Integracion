import { Box, SxProps, Theme } from '@mui/material';

interface BasketballCourtPatternProps {
  sx?: SxProps<Theme>;
}

/**
 * Decorative full-court basketball diagram (boundary, center circle, both
 * keys, both free-throw circles, both three-point arcs) drawn as inline SVG
 * so it needs no external image asset. Meant as a low-opacity background
 * layer, not a literal court — stroke-only, no fill, scales to its container.
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

      <rect x={10} y={160} width={190} height={180} />
      <circle cx={200} cy={250} r={70} />
      <path d="M 10 40 A 430 430 0 0 1 10 460" />

      <rect x={740} y={160} width={190} height={180} />
      <circle cx={740} cy={250} r={70} />
      <path d="M 930 40 A 430 430 0 0 0 930 460" />
    </Box>
  );
}
