import { Box, Divider, Typography } from '@mui/material';
import { ReactNode } from 'react';
import theme from '@/theme';

interface ErrorPageLayoutProps {
  code: string | number;
  children: ReactNode;
}

const ErrorPageLayout: React.FC<ErrorPageLayoutProps> = ({
  code,
  children,
}) => (
  <Box
    sx={{
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '100vh',
      textAlign: 'center',
      backgroundColor: '#fff',
      padding: 3,
    }}
  >
    <Typography
      variant="h1"
      sx={{
        fontSize: { xs: '6rem', sm: '9rem' },
        fontWeight: 700,
        color: theme.palette.primary.main,
        lineHeight: 1,
      }}
    >
      {code}
    </Typography>

    <Divider
      sx={{
        width: 60,
        borderBottomWidth: 3,
        borderColor: theme.palette.primary.main,
        my: 3,
      }}
    />

    {children}
  </Box>
);

export default ErrorPageLayout;
