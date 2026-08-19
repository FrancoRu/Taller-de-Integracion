import { PropsWithChildren } from 'react';
import { ThemeProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { getTheme } from '@/theme';

const theme = getTheme('dark');

const ThemedProvider = ({ children }: PropsWithChildren) => (
  <ThemeProvider theme={theme}>
    <CssBaseline />
    {children}
  </ThemeProvider>
);

export default ThemedProvider;
