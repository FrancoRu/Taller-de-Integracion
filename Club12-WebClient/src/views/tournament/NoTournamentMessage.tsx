import { Typography } from '@mui/material';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';
import React from 'react';

export const NoTournamentMessage: React.FC = () => (
  <CustomBox>
    <Typography>No se encontraron torneos cargados todavía.</Typography>
  </CustomBox>
);
