import { Box, BoxProps } from '@mui/material';

export const CustomBox = (props: BoxProps) => (
  <Box
    {...props}
    sx={{
      mx: 'auto',
      px: 2,
      boxSizing: 'border-box',
      width: {
        xs: '100%',
        md: '50%',
      },
      ...props.sx,
    }}
  />
);
