import { Box, Skeleton } from '@mui/material';

/**
 * A placeholder for a detail view: a title bar, a few paragraph lines and a
 * larger content block, sized to roughly match the loaded detail so the page
 * does not shift when the data arrives.
 */
export default function DetailSkeleton() {
  const lineKeys = Array.from({ length: 4 }, (_, i) => i);
  const widths = ['92%', '85%', '78%', '60%'];

  return (
    <Box role="status" aria-label="Cargando" aria-busy="true">
      <Skeleton variant="text" width="40%" height={40} sx={{ mb: 2 }} />
      {lineKeys.map(line => (
        <Skeleton
          key={line}
          variant="text"
          width={widths[line]}
          height={20}
        />
      ))}
      <Skeleton
        variant="rectangular"
        height={200}
        sx={{ borderRadius: 2, mt: 3 }}
      />
    </Box>
  );
}
