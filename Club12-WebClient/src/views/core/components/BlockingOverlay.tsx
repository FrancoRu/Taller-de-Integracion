import { Backdrop, CircularProgress, Stack, Typography } from '@mui/material';

interface BlockingOverlayProps {
  open: boolean;
  /** Optional message shown under the spinner. */
  message?: string;
}

/**
 * A full-screen blocking overlay with a spinner, shown while a long/destructive
 * operation runs (starting a tournament, reverting to draft, …). It sits above
 * every MUI modal layer so the whole UI is inert until the work finishes.
 */
export default function BlockingOverlay({ open, message }: BlockingOverlayProps) {
  return (
    <Backdrop
      open={open}
      sx={{
        color: '#fff',
        zIndex: theme => theme.zIndex.modal + 1,
        flexDirection: 'column',
      }}
    >
      <Stack spacing={2} sx={{ alignItems: 'center' }}>
        <CircularProgress color="inherit" />
        {message && (
          <Typography variant="body1" sx={{ textAlign: 'center', px: 3 }}>
            {message}
          </Typography>
        )}
      </Stack>
    </Backdrop>
  );
}
