import React, { useState } from 'react'

import formData from '../../data/readJSON.json'

import { Button } from '../button/button'
import { Select } from './Select'
import { Input } from './Input'
import { BaseInputJSON, InputTypesEnum } from '../../types/form/form'

export const MyForm = ({
	handleSubmit,
}: {
	handleSubmit: (event: React.FormEvent<HTMLFormElement>) => void
}) => {
	const [data] = useState<BaseInputJSON[]>(formData.data)

	return (
		<form onSubmit={handleSubmit}>
			{data.map((inputData: BaseInputJSON) =>
				inputData.type === InputTypesEnum.Select ? (
					<Select key={inputData.name} data={inputData} />
				) : (
					<Input key={inputData.name} data={inputData} />
				)
			)}
			<Button variant="contained" value="send" type="submit" />
		</form>
	)
}
