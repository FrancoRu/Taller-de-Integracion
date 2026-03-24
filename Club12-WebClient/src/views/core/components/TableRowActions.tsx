import { ReactNode } from 'react';
import { GridColDef, GridValidRowModel } from '@mui/x-data-grid';
import { IconButton, IconButtonProps, Stack, Tooltip } from '@mui/material';

export interface TableRowAction<Row> {
  label: string;
  icon: ReactNode;
  color?: IconButtonProps['color'];
  onClick: (row: Row) => void;
  disabled?: boolean | ((row: Row) => boolean);
  hidden?: boolean | ((row: Row) => boolean);
}

interface TableRowActionsProps<Row> {
  row: Row;
  actions: TableRowAction<Row>[];
  spacing?: number;
}

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

const resolveRowValue = <Row,>(
  value: boolean | ((row: Row) => boolean) | undefined,
  row: Row,
  fallback: boolean
): boolean => {
  if (typeof value === 'function') {
    return value(row);
  }

  return value ?? fallback;
};

const TableRowActions = <Row,>({
  row,
  actions,
  spacing = 0.5,
}: TableRowActionsProps<Row>) => {
  const visibleActions = actions.filter(
    action => !resolveRowValue(action.hidden, row, false)
  );

  return (
    <Stack direction="row" spacing={spacing}>
      {visibleActions.map(action => {
        const disabled = resolveRowValue(action.disabled, row, false);

        return (
          <Tooltip key={action.label} title={action.label}>
            <span>
              <IconButton
                size="small"
                color={action.color ?? 'primary'}
                disabled={disabled}
                onClick={() => action.onClick(row)}
              >
                {action.icon}
              </IconButton>
            </span>
          </Tooltip>
        );
      })}
    </Stack>
  );
};

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

export default TableRowActions;
