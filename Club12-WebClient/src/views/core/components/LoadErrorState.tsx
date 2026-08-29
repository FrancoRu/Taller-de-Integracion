import { Box, Button, Typography } from '@mui/material';

export interface LoadErrorStateProps {
  /**
   * The message shown above the retry button. Defaults to a generic Spanish
   * load-failure line.
   */
  message?: string;
  /** Re-runs the failed fetch. Rendered as a real "Reintentar" button. */
  onRetry: () => void;
}

const DEFAULT_MESSAGE =
  'No pudimos cargar la información. Revisá tu conexión e intentá de nuevo.';

/**
 * A quiet, inline load-error block for public pages. When an initial GET fails
 * we render this in place of the content — a short Spanish message plus a real
 * "Reintentar" button that re-runs the fetch — instead of firing the global
 * blocking alert, so the page shell stays put and the reader can simply retry.
 */
export default function LoadErrorState({
  message = DEFAULT_MESSAGE,
  onRetry,
}: LoadErrorStateProps) {
  return (
    <Box
      role="alert"
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-start',
        gap: 2,
        py: 4,
      }}
    >
      <Typography sx={{ color: 'text.secondary' }}>{message}</Typography>
      <Button variant="outlined" onClick={onRetry}>
        Reintentar
      </Button>
    </Box>
  );
}
