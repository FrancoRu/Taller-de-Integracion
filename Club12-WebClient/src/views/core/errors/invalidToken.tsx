import { Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';

export default function InvalidToken() {
  return (
    <ErrorPageLayout code={401}>
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: grey[800], mb: 1 }}
      >
        Sesión expirada o inválida
      </Typography>
      <Typography variant="body1" sx={{ color: grey[500], maxWidth: 380 }}>
        El token de autenticación no es válido o ha expirado.
      </Typography>
      <ErrorPageActions showLogin />
    </ErrorPageLayout>
  );
}
