import { Typography } from '@mui/material';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';

export const NoStagesMessage: React.FC<{ name: string }> = ({ name }) => (
  <CustomBox>
    <Typography>
      No se encontraron fechas cargadas para la division {name} todavía
    </Typography>
  </CustomBox>
);
