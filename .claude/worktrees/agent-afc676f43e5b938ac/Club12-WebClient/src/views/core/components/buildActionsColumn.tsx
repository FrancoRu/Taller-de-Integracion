import { GridColDef, GridValidRowModel } from '@mui/x-data-grid';
import TableRowActions, { TableRowAction } from './TableRowActions';

interface BuildActionsColumnOptions<Row extends GridValidRowModel>
  extends Partial<
    Pick<
      GridColDef<Row>,
      | 'field'
      | 'headerName'
      | 'minWidth'
      | 'width'
      | 'flex'
      | 'align'
      | 'headerAlign'
    >
  > {
  spacing?: number;
}

export const buildActionsColumn = <Row extends GridValidRowModel>(
  actions: TableRowAction<Row>[],
  options: BuildActionsColumnOptions<Row> = {}
): GridColDef<Row> => ({
  field: options.field ?? 'actions',
  headerName: options.headerName ?? 'Acciones',
  sortable: false,
  filterable: false,
  minWidth: options.minWidth ?? 120,
  width: options.width,
  flex: options.flex,
  align: options.align,
  headerAlign: options.headerAlign,
  renderCell: params => (
    <TableRowActions
      row={params.row}
      actions={actions}
      spacing={options.spacing}
    />
  ),
});
