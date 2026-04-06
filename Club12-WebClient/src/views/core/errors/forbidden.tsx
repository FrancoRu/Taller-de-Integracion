import { Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';

export default function Forbidden() {
  return (
    <ErrorPageLayout code={403}>
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: grey[800], mb: 1 }}
      >
        Acceso denegado
      </Typography>
      <Typography variant="body1" sx={{ color: grey[500], maxWidth: 380 }}>
        No tienes permisos para acceder a este recurso.
      </Typography>
      <ErrorPageActions showLogin />
    </ErrorPageLayout>
  );
}
