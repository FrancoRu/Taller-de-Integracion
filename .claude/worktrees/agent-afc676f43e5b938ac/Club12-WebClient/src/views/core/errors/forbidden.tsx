import { Typography } from '@mui/material';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export default function Forbidden() {
  return (
    <ErrorPageLayout code={HttpStatus.Forbidden}>
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: 'text.primary', mb: 1 }}
      >
        Acceso denegado
      </Typography>
      <Typography variant="body1" sx={{ color: 'text.secondary', maxWidth: 380 }}>
        No tienes permisos para acceder a este recurso.
      </Typography>
      <ErrorPageActions showLogin />
    </ErrorPageLayout>
  );
}
