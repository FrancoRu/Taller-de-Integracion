import { Typography } from '@mui/material';
import React from 'react';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';

export const NoVenueMessage: React.FC = () => (
  <CustomBox>
    <Typography>No se encontraron canchas cargadas todavía.</Typography>
  </CustomBox>
);
