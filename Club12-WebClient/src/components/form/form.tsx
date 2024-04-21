import '../../styles/form/form.css'
import { BaseInputJSON, InputTypesEnum } from '../../types/form/form.d'
import { Select } from './select'
import { Input } from './input'
import { Button } from '../button/button'
import { IForm } from '../../types/form/form'
import React from 'react'

export const Form: React.FC<IForm> = ({ handleSubmit, value, data }) => {
	return (
		<form className='form' onSubmit={handleSubmit}>
			{data.map((inputData: BaseInputJSON) =>
				inputData.type === InputTypesEnum.Select ? (
					<Select key={inputData.name} data={inputData} />
				) : (
					<Input key={inputData.name} data={inputData} />
				)
			)}
			<Button value={value} type='submit' />
		</form>
	)
}
