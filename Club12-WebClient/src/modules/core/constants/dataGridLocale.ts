import { esES } from '@mui/x-data-grid/locales';

/**
 * Spanish (es-ES) localeText for the MUI X DataGrid — pager ("Filas por
 * página", "de"), filters, column menu, etc.
 *
 * The theme already merges `esES` into
 * `theme.components.MuiDataGrid.defaultProps.localeText`, which covers grids
 * that do NOT pass their own `localeText`. But MUI's `resolveProps` only fills
 * a prop from `defaultProps` when the component leaves it `undefined`: a page
 * that passes `localeText={{ noRowsLabel }}` shallow-REPLACES the theme's
 * Spanish localeText, so the footer falls back to the English defaults.
 *
 * Any DataGrid that needs a custom empty-rows message must therefore spread
 * this constant into its `localeText` prop. `dataGridLocaleText(noRowsLabel)`
 * does exactly that in a single call.
 */
export const DATA_GRID_ES_LOCALE_TEXT =
  esES.components.MuiDataGrid.defaultProps.localeText;

/** Spanish DataGrid localeText merged with a page-specific empty-rows label. */
export const dataGridLocaleText = (
  noRowsLabel: string
): typeof DATA_GRID_ES_LOCALE_TEXT => ({
  ...DATA_GRID_ES_LOCALE_TEXT,
  noRowsLabel,
});
