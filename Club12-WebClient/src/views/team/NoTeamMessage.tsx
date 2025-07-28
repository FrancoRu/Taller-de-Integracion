import { Typography } from '@mui/material';
import React from 'react';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';

export const NoTeamMessage: React.FC = () => (
  <CustomBox>
    <Typography>No se encontraron equipos cargados todavía.</Typography>
  </CustomBox>
);
