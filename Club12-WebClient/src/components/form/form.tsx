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
}
