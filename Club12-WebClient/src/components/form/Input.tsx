import { TextField } from '@mui/material'
import React from 'react'
import { IInput } from '../../types/form/form'

export const Input: React.FC<IInput> = ({ data }) => {
	return (
		<TextField
			name={data.name}
			type={data.type}
			placeholder={data.input?.placeholder ?? ''}
			label={data.label}
			id={data.id}
			variant='outlined'
			required={data.input?.required ?? true}
			aria-readonly={data.input?.readonly ?? false}
			disabled={data.input?.disabled ?? false}
		/>
	)
}
