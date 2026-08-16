import { Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import ErrorPageLayout from '@/views/core/components/ErrorPageLayout';
import ErrorPageActions from '@/views/core/components/ErrorPageActions';
import { HttpStatus } from '@/modules/core/constants/httpStatus';

export default function NotFound() {
  return (
    <ErrorPageLayout code={HttpStatus.NotFound}>
      <Typography
        variant="h5"
        sx={{ fontWeight: 600, color: grey[800], mb: 1 }}
      >
        Página no encontrada
      </Typography>
      <Typography variant="body1" sx={{ color: grey[500], maxWidth: 380 }}>
        La página que estás buscando no existe o fue movida.
      </Typography>
      <ErrorPageActions />
    </ErrorPageLayout>
  );
}
