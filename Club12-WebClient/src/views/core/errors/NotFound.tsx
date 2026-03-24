import { Typography } from '@mui/material';
import { grey } from '@mui/material/colors';
import ErrorPageLayout from '../components/ErrorPageLayout';
import ErrorPageActions from '../components/ErrorPageActions';

export default function NotFound() {
  return (
    <ErrorPageLayout code={404}>
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
