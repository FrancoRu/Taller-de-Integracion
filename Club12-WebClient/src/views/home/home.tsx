import { DataTable } from '../../components/table/table'
import data from '../../data/readJSON.json'
import '../../styles/home/home.css'
export const Home = () => {
	return (
		<div className='home'>
			<DataTable data={data['tabla-preba']} />
		</div>
	)
}
