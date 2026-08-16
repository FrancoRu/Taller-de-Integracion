import { PropsWithChildren } from 'react';
import { ThemeProvider } from '@emotion/react';
import theme from '@/theme';

const ThemedProvider = ({ children }: PropsWithChildren) => (
  <ThemeProvider theme={theme}>{children}</ThemeProvider>
);

export default ThemedProvider;
