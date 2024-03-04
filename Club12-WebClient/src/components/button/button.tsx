import { Button as MUIButton } from '@mui/material'
import { IButton } from '../../types/buttons/IButton'
import React from 'react'

export const Button: React.FC<IButton> = ({
	variant,
	type,
	color,
	handler,
	classname,
	value,
	dataTarget
}) => {
	const handlerClick = (event: React.MouseEvent<HTMLButtonElement>) => {
		const target = event.currentTarget.getAttribute('data-target')
		if (target === null) return
		handler?.(target)
	}

	return (
		<MUIButton
			data-target={dataTarget}
			variant={variant ?? 'contained'}
			type={type}
			color={color ?? 'secondary'}
			onClick={handlerClick}
			className={classname}
		>
			{value}
		</MUIButton>
	)
}
