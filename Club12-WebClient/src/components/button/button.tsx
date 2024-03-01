import { Button as MUIButton } from '@mui/material'

interface IButton {
	type: 'submit' | 'reset' | 'button'
	value: string
	variant?: 'text' | 'outlined' | 'contained'
	color?: 'secondary' | 'error' | 'success'
	classname?: string
	handler?: (param?: string) => void
}

export const Button: React.FC<IButton> = (props) => {
	return (
		<MUIButton
			variant={props.variant ?? 'contained'}
			type={props.type}
			color={props.color ?? 'secondary'}
			onClick={() => props.handler?.()}
			className={props.classname}
		>
			{props.value}
		</MUIButton>
	)
}
