import Button from '@mui/material/Button'
import { IButton } from '../../types/button/IButton'

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
