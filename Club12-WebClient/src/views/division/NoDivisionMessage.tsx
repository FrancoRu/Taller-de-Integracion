import { Typography } from '@mui/material';

export const NoDivisionMessage: React.FC<{ name: string }> = ({ name }) => {
  return (
    <Typography>No se encontraron division para el Torneo: {name}</Typography>
  );
};
