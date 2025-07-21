import { Box, CircularProgress, Typography } from '@mui/material';

const LoadingIndicator = () => (
  <Box
    display="flex"
    alignItems="center"
    justifyContent="center"
    gap={2}
    padding={2}
  >
    <CircularProgress size={24} />
    <Typography>Cargando...</Typography>
  </Box>
);

export default LoadingIndicator;
