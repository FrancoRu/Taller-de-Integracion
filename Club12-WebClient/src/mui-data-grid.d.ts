import {
  ComponentsOverrides,
  ComponentsProps,
  ComponentsVariants,
} from '@mui/material/styles';
import { DataGridProps } from '@mui/x-data-grid';

declare module '@mui/material/styles' {
  interface Components {
    MuiDataGrid?: {
      defaultProps?: ComponentsProps['MuiDataGrid'];
      styleOverrides?: ComponentsOverrides<Theme>['MuiDataGrid'];
      variants?: ComponentsVariants['MuiDataGrid'];
    };
  }
}
