import { Box, Skeleton, Stack } from '@mui/material';

export interface ListSkeletonProps {
  /** Number of list rows to render. */
  items?: number;
}

/**
 * A placeholder shaped like a list of records: each row is an avatar circle
 * beside two text lines, sized to roughly match the real rows so the loaded
 * list does not shift the layout.
 */
export default function ListSkeleton({ items = 6 }: ListSkeletonProps) {
  const itemKeys = Array.from({ length: items }, (_, i) => i);

  return (
    <Stack spacing={1.5} role="status" aria-label="Cargando" aria-busy="true">
      {itemKeys.map(item => (
        <Box key={item} sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Skeleton variant="circular" width={40} height={40} />
          <Box sx={{ flex: 1 }}>
            <Skeleton variant="text" width="45%" height={20} />
            <Skeleton variant="text" width="70%" height={16} />
          </Box>
        </Box>
      ))}
    </Stack>
  );
}
