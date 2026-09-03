import type { ReactNode } from 'react';
import { Box } from '@mui/material';

interface TableScrollBoxProps {
  children: ReactNode;
}

/**
 * Wraps a DataGrid (or any wide table) in its own horizontally-scrollable
 * box, so a table with more columns than the viewport can hold scrolls
 * within itself instead of pushing the whole page wider — without this, a
 * table's intrinsic content width bled through `width: '100%'` into every
 * ancestor with no `overflow` boundary of its own, so the page's filter bar
 * and header ended up just as wide as the table even though they were each
 * individually laid out responsively. A slim, always-visible scrollbar
 * (same treatment as `PlayoffBracket`) hints there's more to see instead of
 * silently clipping columns off-screen with no affordance.
 */
export default function TableScrollBox({ children }: TableScrollBoxProps) {
  return (
    <Box
      sx={{
        width: '100%',
        overflowX: 'auto',
        scrollbarWidth: 'thin',
        '&::-webkit-scrollbar': { height: 8 },
        '&::-webkit-scrollbar-thumb': {
          backgroundColor: 'action.disabled',
          borderRadius: 4,
        },
      }}
    >
      {children}
    </Box>
  );
}
