import { DataTable } from '../../components/table/table'
import data from '../../data/readJSON.json'

export const Tournament = () => {
	return (
		<>
			<h2>Torneos</h2>
			<DataTable data={data['tabla-prueba']} />
		</>
	)
}
