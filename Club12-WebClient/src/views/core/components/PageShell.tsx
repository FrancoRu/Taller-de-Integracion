import type { ElementType, ReactNode } from 'react';
import { Box, Button, Container, Typography } from '@mui/material';
import { pageMinHeight } from '@/design/tokens';

export interface PageShellBack {
  /** Visible label for the back affordance (rendered as "← {label}"). */
  label: string;
  onClick: () => void;
}

export interface PageShellProps {
  /** Page heading, rendered as the Oswald `<h1>` when present. */
  title?: string;
  /** Trailing controls (buttons, menus) aligned to the right of the title. */
  actions?: ReactNode;
  /** Optional back affordance rendered above the title. */
  back?: PageShellBack;
  /** MUI Container width. `false` opts out of a max width. Defaults to `false` (full width). */
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | false;
  /** Wrapper element for the Container. Defaults to `div` (never `main`, since
   *  the layouts already render the page's single `<main>`). */
  component?: ElementType;
  children: ReactNode;
}

/**
 * The consistent content container every page renders inside. It reserves a
 * constant `minHeight` (from the design tokens) so a view is the same height
 * while its data loads (skeleton) and once it arrives — no layout jump — and
 * offers an optional header region (back button + title + actions) that stacks
 * responsively on small screens.
 *
 * It deliberately does NOT render a `<main>`: `PublicLayout` and
 * `SidebarLayout` already own the page's single `<main>` landmark, so the
 * default wrapper here is a plain `div` (overridable via `component`).
 */
export default function PageShell({
  title,
  actions,
  back,
  maxWidth = false,
  component = 'div',
  children,
}: PageShellProps) {
  const hasHeader = Boolean(title || actions || back);

  return (
    <Container
      component={component}
      maxWidth={maxWidth}
      sx={{ py: { xs: 2, sm: 3 }, minHeight: pageMinHeight }}
    >
      {hasHeader && (
        <Box component="header" sx={{ mb: 3 }}>
          {back && (
            <Button
              variant="text"
              onClick={back.onClick}
              sx={{ mt: 0, mb: 1, px: 0, minWidth: 0 }}
            >
              ← {back.label}
            </Button>
          )}
          <Box
            sx={{
              display: 'flex',
              flexDirection: { xs: 'column', sm: 'row' },
              alignItems: { xs: 'flex-start', sm: 'center' },
              // With no `title` (a page rendering its own heading in
              // children), `actions` is the row's only flex child —
              // space-between then collapses it to the start instead of
              // the end, misaligning every action row on that page.
              justifyContent: title ? 'space-between' : 'flex-end',
              gap: 2,
            }}
          >
            {title && (
              <Typography variant="h4" component="h1">
                {title}
              </Typography>
            )}
            {actions && (
              <Box
                sx={{
                  display: 'flex',
                  gap: 1,
                  flexWrap: 'wrap',
                  alignItems: 'center',
                }}
              >
                {actions}
              </Box>
            )}
          </Box>
        </Box>
      )}
      {children}
    </Container>
  );
}
