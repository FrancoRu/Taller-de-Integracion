import { useRouteError } from 'react-router-dom';
import { Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';

interface ErrorDetails {
  statusText?: string;
  message: string;
}

export default function ErrorPage() {
  const error = useRouteError() as ErrorDetails | { message: string };
  const message =
    (error as ErrorDetails).statusText || (error as ErrorDetails).message;

  return (
    <ErrorPageLayout code="Error">
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: grey[800], mb: 1 }}
      >
        Ocurrió un error inesperado
      </Typography>
      {message && (
        <Typography variant="body1" sx={{ color: grey[500], maxWidth: 400 }}>
          {message}
        </Typography>
      )}
      <ErrorPageActions />
    </ErrorPageLayout>
  );
}
