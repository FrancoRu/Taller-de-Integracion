import '@fontsource/roboto/300.css'
import '@fontsource/roboto/400.css'
import '@fontsource/roboto/500.css'
import '@fontsource/roboto/700.css'

import { DataTable } from './components/table/table'

const personas: Record<string, any>[] = [
	{
		id: 1,
		nombre: 'Juan',
		apellido: 'Pérez',
		edad: 25,
		ciudad: 'Ciudad A',
	},
	{
		id: 2,
		nombre: 'María',
		apellido: 'Gómez',
		edad: 30,
		ciudad: 'Ciudad B',
	},
	{
		id: 3,
		nombre: 'Carlos',
		apellido: 'López',
		edad: 22,
		ciudad: 'Ciudad C',
	},
]

function App() {
	return (
		<>
			<DataTable data={personas} />
		</>
	)
}

export default App
