<<<<<<< HEAD
import { DataGrid, GridColDef } from '@mui/x-data-grid'

interface ITableProps {
	data: Record<string, any>[]
	style?: {
		height?: string | number
		width?: string | number
	}
}

export const DataTable: React.FC<ITableProps> = ({ data, style }) => {
	const headers = Object.keys(data[0]).filter((key) => key !== 'id')

	const columns: GridColDef[] = headers.map((header) => ({
		field: header,
		headerName: header,
	}))

	const rows = data.map((elements) => {
		const row: Record<string, any> = { id: elements.id }
		headers.forEach((header) => {
			if (header !== 'id') {
				row[header] = elements[header]
			}
		})
		return row
	})

	return (
		<div style={style ? { ...style } : undefined}>
			<DataGrid
				rows={rows}
				columns={columns}
				initialState={{
					pagination: {
						paginationModel: {
							page: 0,
							pageSize: data.length < 5 ? data.length : 5,
						},
					},
				}}
				checkboxSelection
			/>
		</div>
	)
}
=======
import { DataGrid, GridColDef } from "@mui/x-data-grid";
import { ITable } from "../../types/tables/ITable";



export const DataTable: React.FC<ITable> = ({ data, style }) => {
  const headers = Object.keys(data[0]).filter((key) => key !== "id");

  const columns: GridColDef[] = headers.map((header) => ({
    field: header,
    headerName: header,
  }));

  const rows = data.map((elements) => {
    const row: Record<string, any> = { id: elements.id };
    headers.forEach((header) => {
      if (header !== "id") {
        row[header] = elements[header];
      }
    });
    return row;
  });

  return (
    <div style={style ? { ...style } : undefined}>
      <DataGrid
        rows={rows}
        columns={columns}
        initialState={{
          pagination: {
            paginationModel: {
              page: 0,
              pageSize: data.length < 5 ? data.length : 5,
            },
          },
        }}
        checkboxSelection
      />
    </div>
  );
};
>>>>>>> d9fb9ed5dde741cf4cabae97290f56c8eaab95b3
