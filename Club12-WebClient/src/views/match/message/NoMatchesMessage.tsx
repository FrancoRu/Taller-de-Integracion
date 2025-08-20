import { Typography } from '@mui/material';
import { CustomBox } from '../../core/MUI/customsThemes/CustomBox';

export const NoMatchesMessage: React.FC<{ name: string }> = ({ name }) => (
  <CustomBox>
    <Typography>
      No se encontraron Partidos cargados para la fase {name} todavía
    </Typography>
  </CustomBox>
);
