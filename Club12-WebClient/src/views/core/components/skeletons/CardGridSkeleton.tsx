import { Grid, Skeleton } from '@mui/material';

export interface CardGridSkeletonProps {
  /** Number of card placeholders to render. */
  count?: number;
}

/**
 * A responsive grid of card-height rectangles, sized to roughly match a grid
 * of loaded cards so swapping in the real content does not shift the layout.
 */
export default function CardGridSkeleton({ count = 6 }: CardGridSkeletonProps) {
  const cardKeys = Array.from({ length: count }, (_, i) => i);

  return (
    <Grid
      container
      spacing={2}
      role="status"
      aria-label="Cargando"
      aria-busy="true"
    >
      {cardKeys.map(card => (
        <Grid key={card} size={{ xs: 12, sm: 6, md: 4 }}>
          <Skeleton
            variant="rectangular"
            height={180}
            sx={{ borderRadius: 2, width: '100%' }}
          />
        </Grid>
      ))}
    </Grid>
  );
}
