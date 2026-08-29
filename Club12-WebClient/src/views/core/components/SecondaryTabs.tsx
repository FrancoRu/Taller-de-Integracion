import { ReactNode, SyntheticEvent } from 'react';
import { Tabs } from '@mui/material';

interface SecondaryTabsProps {
  value: string;
  onChange: (event: SyntheticEvent, value: string) => void;
  children: ReactNode;
  'aria-label'?: string;
}

/**
 * Second-level navigation styled as a compact "segmented control" (pills),
 * deliberately different from the underline `Tabs` used for the primary level
 * above it. Two visually distinct tab styles let a visitor tell at a glance
 * which choice is the section (division) and which is the view within it
 * (standings, fixtures, ...). Pass `Tab` children with string `value`s.
 */
export default function SecondaryTabs({
  value,
  onChange,
  children,
  'aria-label': ariaLabel,
}: SecondaryTabsProps) {
  return (
    <Tabs
      value={value}
      onChange={onChange}
      variant="scrollable"
      scrollButtons="auto"
      aria-label={ariaLabel}
      sx={{
        minHeight: 0,
        mb: 3,
        '& .MuiTabs-indicator': { display: 'none' },
        '& .MuiTabs-flexContainer': { gap: 1 },
        '& .MuiTab-root': {
          minHeight: 36,
          py: 0.5,
          px: 2,
          borderRadius: 999,
          textTransform: 'none',
          fontWeight: 600,
          fontSize: '0.85rem',
          color: 'text.secondary',
          border: '1px solid',
          borderColor: 'divider',
          transition: 'background-color 0.15s, color 0.15s, border-color 0.15s',
          '&:hover': { borderColor: 'primary.main', color: 'text.primary' },
          '&.Mui-selected': {
            bgcolor: 'primary.main',
            color: 'primary.contrastText',
            borderColor: 'primary.main',
          },
        },
      }}
    >
      {children}
    </Tabs>
  );
}
