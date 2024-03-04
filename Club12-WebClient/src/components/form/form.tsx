<<<<<<< HEAD
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
=======
import React from "react"
import '../../styles/form/form.css'
import { type BaseInputJSON, InputTypesEnum } from "../../types/forms/form.d"
import { Select } from "./select"
import { Input } from "./input"
import { Button } from "../button/button"

export const MyForm = ({
  handleSubmit,
  data,
  value
}: {
  handleSubmit: (event: React.FormEvent<HTMLFormElement>) => void
  data: BaseInputJSON[]
  value: string
}) => {
  return (
    <form className='form' onSubmit={handleSubmit}>
      {data.map((inputData: BaseInputJSON) =>
        inputData.type === InputTypesEnum.Select
          ? (
          <Select key={inputData.name} data={inputData} />
            )
          : (
          <Input key={inputData.name} data={inputData} />
            )
      )}
      <Button value={value} type="submit" />
    </form>
  )
>>>>>>> d9fb9ed5dde741cf4cabae97290f56c8eaab95b3
}
