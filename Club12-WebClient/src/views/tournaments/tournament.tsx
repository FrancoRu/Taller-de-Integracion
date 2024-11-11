import { useEffect } from 'react'
import { DataTable } from '../../components/table/table'
import data from '../../data/readJSON.json'

export const Tournament = () => {

	useEffect(() => {
		fetch('../../data/mockSeason.json')
			.then(res => console.log(res))
	}, [])
	return (
		<>
			<h2>Torneos</h2>

		</>
	)
}
