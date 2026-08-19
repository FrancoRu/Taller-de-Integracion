import {
  ComponentsOverrides,
  ComponentsProps,
  ComponentsVariants,
} from '@mui/material/styles';

declare module '@mui/material/styles' {
  interface Components {
    MuiDataGrid?: {
      defaultProps?: ComponentsProps['MuiDataGrid'];
      styleOverrides?: ComponentsOverrides<Theme>['MuiDataGrid'];
      variants?: ComponentsVariants['MuiDataGrid'];
    };
  }
}
