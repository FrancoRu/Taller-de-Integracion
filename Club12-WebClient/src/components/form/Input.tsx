<<<<<<< HEAD
import { TextField } from '@mui/material'
import { BaseInputJSON } from '../../types/form/form'

export const Input = ({ data }: { data: BaseInputJSON }) => {
	return (
		<TextField
			name={data.name}
			placeholder={data.input?.placeholder ?? ''}
			label={data.label}
			id={data.id}
			variant="outlined"
			required={data.input?.required ?? true}
			aria-readonly={data.input?.readonly ?? false}
			disabled={data.input?.disabled ?? false}
		/>
	)
}
=======
import { TextField } from "@mui/material";
import { BaseInputJSON } from "../../types/forms/form.d";

export const Input = ({ data }: { data: BaseInputJSON }) => {
  return (
    <TextField
      name={data.name}
      type={data.type}
      placeholder={data.input?.placeholder ?? ""}
      label={data.label}
      id={data.id}
      variant="outlined"
      required={data.input?.required ?? true}
      aria-readonly={data.input?.readonly ?? false}
      disabled={data.input?.disabled ?? false}
      
    />
  );
};
>>>>>>> d9fb9ed5dde741cf4cabae97290f56c8eaab95b3
