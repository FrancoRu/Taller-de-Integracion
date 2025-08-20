import { CustomBox } from '@/views/core/MUI/customsThemes/CustomBox';
import { Typography } from '@mui/material';

export const NoMatchMessage = () => (
  <CustomBox>
    <Typography>
      No se encontro el partido seleccionado. Intentelo mas tarde.
    </Typography>
  </CustomBox>
);
