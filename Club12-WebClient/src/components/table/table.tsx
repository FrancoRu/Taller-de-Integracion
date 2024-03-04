import { DataGrid, GridColDef } from '@mui/x-data-grid'
import { ITable } from '../../types/tables/ITable'

export const DataTable: React.FC<ITable> = ({ data, style }) => {
	const headers = Object.keys(data[0]).filter((key) => key !== 'id')

	const columns: GridColDef[] = headers.map((header) => ({
		field: header,
		headerName: header
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
							pageSize: data.length < 5 ? data.length : 5
						}
					}
				}}
				checkboxSelection
			/>
		</div>
	)
}
