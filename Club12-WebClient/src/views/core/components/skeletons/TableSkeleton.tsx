import { Box, Skeleton } from '@mui/material';

export interface TableSkeletonProps {
  /** Number of body rows to render. */
  rows?: number;
  /** Number of columns per row. */
  columns?: number;
}

/**
 * A placeholder shaped like a data table (a header row plus body rows of
 * rectangular cells), sized to roughly match real content so swapping in the
 * loaded table does not shift the layout.
 */
export default function TableSkeleton({
  rows = 5,
  columns = 4,
}: TableSkeletonProps) {
  const columnKeys = Array.from({ length: columns }, (_, i) => i);
  const rowKeys = Array.from({ length: rows }, (_, i) => i);

  return (
    <Box role="status" aria-label="Cargando" aria-busy="true">
      <Box sx={{ display: 'flex', gap: 2, mb: 1.5 }}>
        {columnKeys.map(col => (
          <Skeleton
            key={`head-${col}`}
            variant="rectangular"
            height={28}
            sx={{ flex: 1, borderRadius: 1 }}
          />
        ))}
      </Box>
      {rowKeys.map(row => (
        <Box key={`row-${row}`} sx={{ display: 'flex', gap: 2, mb: 1 }}>
          {columnKeys.map(col => (
            <Skeleton
              key={`cell-${row}-${col}`}
              variant="rectangular"
              height={44}
              sx={{ flex: 1, borderRadius: 1 }}
            />
          ))}
        </Box>
      ))}
    </Box>
  );
}
