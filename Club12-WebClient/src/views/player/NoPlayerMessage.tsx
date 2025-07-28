import { Typography } from '@mui/material';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';

export const NoPlayerMessage: React.FC<{ name: string }> = ({ name }) => (
  <CustomBox>
    <Typography>
      No se encontraron jugadores cargados para el equipo {name} todavía.
    </Typography>
  </CustomBox>
);
