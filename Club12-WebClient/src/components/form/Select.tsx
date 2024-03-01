import { useState } from 'react'
import {
	FormControl,
	InputLabel,
	Select as SelectMUI,
	MenuItem,
	SelectChangeEvent,
} from '@mui/material'
import { BaseInputJSON, SelectOption } from '../../types/form/form'

export const Select = ({ data }: { data: BaseInputJSON }) => {
	const [option, setOption] = useState<string>('')
	const handleChange = (event: SelectChangeEvent<string>) => {
		setOption(event.target.value)
	}
	return (
		data.options && (
			<FormControl variant="outlined" fullWidth margin="normal">
				<InputLabel htmlFor={data.id}>{data.label}</InputLabel>
				<SelectMUI
					labelId={data.id}
					label={data.label}
					name={data.name}
					value={option}
					onChange={handleChange}
				>
					{data.options.map(
						(option: SelectOption) =>
							option && (
								<MenuItem value={option.value} key={option.value}>
									{option.label}
								</MenuItem>
							)
					)}
				</SelectMUI>
			</FormControl>
		)
	)
}
