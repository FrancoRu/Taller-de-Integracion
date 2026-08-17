import { Typography } from '@mui/material';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export default function InvalidToken() {
  return (
    <ErrorPageLayout code={HttpStatus.Unauthorized}>
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: 'text.primary', mb: 1 }}
      >
        Sesión expirada o inválida
      </Typography>
      <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 380 }}>
        El token de autenticación no es válido o ha expirado.
      </Typography>
      <ErrorPageActions showLogin />
    </ErrorPageLayout>
  );
}
