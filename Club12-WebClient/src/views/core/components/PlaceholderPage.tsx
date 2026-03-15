import React from 'react';
import { Box, Typography } from '@mui/material';

interface PlaceholderPageProps {
  title: string;
}

const PlaceholderPage: React.FC<PlaceholderPageProps> = ({ title }) => (
  <Box sx={{ p: 4 }}>
    <Typography variant="h4" fontWeight={600}>
      {title}
    </Typography>
  </Box>
);

export default PlaceholderPage;
