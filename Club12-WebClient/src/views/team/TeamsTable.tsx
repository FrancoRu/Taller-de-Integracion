import { Box } from '@mui/material';
import { DataGrid, GridColDef, GridPaginationModel } from '@mui/x-data-grid';
import type { ITeamResponse } from '@/modules/team/type/team.d';
import { dataGridLocaleText } from '@/modules/core/constants/dataGridLocale';

interface TeamsTableProps {
  rows: ITeamResponse[];
  columns: GridColDef<ITeamResponse>[];
  loading: boolean;
  noRowsMessage: string;
  paginationModel: GridPaginationModel;
  onPaginationModelChange: (model: GridPaginationModel) => void;
  pageSizeOptions: number[];
  rowCount: number;
}

const TeamsTable: React.FC<TeamsTableProps> = ({
  rows,
  columns,
  loading,
  noRowsMessage,
  paginationModel,
  onPaginationModelChange,
  pageSizeOptions,
  rowCount,
}) => (
  <Box sx={{ width: '100%' }}>
    <DataGrid
      rows={rows}
      columns={columns}
      loading={loading}
      getRowId={row => row.id}
      autoHeight
      disableRowSelectionOnClick
      disableColumnMenu
      localeText={dataGridLocaleText(noRowsMessage)}
      pageSizeOptions={pageSizeOptions}
      paginationModel={paginationModel}
      onPaginationModelChange={onPaginationModelChange}
      paginationMode="server"
      rowCount={rowCount}
    />
  </Box>
);

export default TeamsTable;
