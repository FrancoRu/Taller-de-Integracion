import "../../styles/form/form.css";
import { BaseInputJSON, IForm, InputTypesEnum } from "../../types/form/form";
import React from "react";
import { Button } from "../Button/Button";
import { Select } from "./Select";
import { Input } from "./Input";


export const Form: React.FC<IForm> = ({
  handleSubmit,
  value,
  data,
  formRef,
}) => {
  return (
    <form ref={formRef} className="form" onSubmit={handleSubmit}>
      {data.map((inputData: BaseInputJSON) =>
        inputData.type === InputTypesEnum.Select ? (
          <Select key={inputData.name} data={inputData} />
        ) : (
          <Input key={inputData.name} data={inputData} />
        )
      )}
      <Button value={value} type="submit" />
    </form>
  );
};
