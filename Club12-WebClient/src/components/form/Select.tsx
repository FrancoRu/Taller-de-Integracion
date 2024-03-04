import React, { useState } from 'react'
import {
	FormControl,
	InputLabel,
	Select as SelectMUI,
	MenuItem,
	type SelectChangeEvent
} from '@mui/material'
import { SelectOption } from '../../types/form/form'
import { IInput } from '../../types/form/form'

export const Select: React.FC<IInput> = ({ data }) => {
	const [option, setOption] = useState<string>('')
	const handleChange = (event: SelectChangeEvent<string>) => {
		setOption(event.target.value)
	}
	return (
		data.options && (
			<FormControl variant='outlined' fullWidth margin='normal'>
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
