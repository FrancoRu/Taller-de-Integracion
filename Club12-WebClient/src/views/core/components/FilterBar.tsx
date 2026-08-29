import type { ReactNode } from 'react';
import { Box, Button } from '@mui/material';

export interface FilterBarProps {
  /** The filter controls (inputs, selects) laid out in a wrapping row. */
  children: ReactNode;
  /** When set, renders a trailing "Limpiar filtros" text button. */
  onClear?: () => void;
  /** Accessible label for the filter region. Defaults to "Filtros". */
  ariaLabel?: string;
}

/**
 * A consistent container for a page's filter controls: a wrapping row of the
 * given controls on a subtle raised surface, with an optional "clear filters"
 * affordance. Rendered as a labelled `<section>` so assistive tech announces
 * the filter region.
 */
export default function FilterBar({
  children,
  onClear,
  ariaLabel = 'Filtros',
}: FilterBarProps) {
  return (
    <Box
      component="section"
      aria-label={ariaLabel}
      sx={{
        p: 2,
        mb: 3,
        borderRadius: 2,
        backgroundColor: 'background.paper',
        border: '1px solid',
        borderColor: 'divider',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexDirection: { xs: 'column', sm: 'row' },
          flexWrap: 'wrap',
          gap: 2,
          alignItems: { xs: 'stretch', sm: 'center' },
        }}
      >
        {children}
        {onClear && (
          <Button variant="text" onClick={onClear} sx={{ mt: 0 }}>
            Limpiar filtros
          </Button>
        )}
      </Box>
    </Box>
  );
}
