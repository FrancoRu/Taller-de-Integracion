import Button from '@mui/material/Button'

interface IButton {
	type: 'submit' | 'reset' | 'button'
	value: string
	variant?: 'text' | 'outlined' | 'contained'
	color?: 'secondary' | 'error' | 'success'
	classname?: string
	handler?: (param?: string) => void
}

export const CButton: React.FC<IButton> = (props) => {
	return (
		<Button
			variant={props.variant ?? 'contained'}
			type={props.type}
			color={props.color ?? 'secondary'}
			onClick={() => props.handler?.()}
			className={props.classname}
		>
			{props.value}
		</Button>
	)
}
