import { ElementType, ReactNode } from 'react';
import { Box, Typography } from '@mui/material';

interface SectionHeadingProps {
  children: ReactNode;
  /** Optional trailing content (e.g. a count chip or an action button). */
  action?: ReactNode;
  /** Semantic element for the heading text. Defaults to `h3`. */
  component?: ElementType;
  /** Optional tint for the accent bar (e.g. a category hue). Defaults to `primary.main`. */
  accentColor?: string;
}

/**
 * A section title with a short orange accent bar to the left. Sits one level
 * below a page's `h1`/PageShell title and above its content, giving lists and
 * tab panels a consistent, scannable subheading instead of a bare line of
 * text. The accent ties the hierarchy to the brand without shouting, and can be
 * tinted (via `accentColor`) to carry a section-specific hue such as a category.
 */
export default function SectionHeading({
  children,
  action,
  component = 'h3',
  accentColor,
}: SectionHeadingProps) {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 2,
        mb: 2,
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, minWidth: 0 }}>
        <Box
          aria-hidden
          sx={{
            width: 4,
            height: 20,
            borderRadius: 999,
            bgcolor: accentColor ?? 'primary.main',
            flexShrink: 0,
          }}
        />
        <Typography
          variant="h6"
          component={component}
          noWrap
          sx={{ fontWeight: 700, letterSpacing: '0.01em' }}
        >
          {children}
        </Typography>
      </Box>
      {action}
    </Box>
  );
}
