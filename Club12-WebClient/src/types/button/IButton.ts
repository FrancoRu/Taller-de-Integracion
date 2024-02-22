export interface IButton {
	type: 'submit' | 'reset' | 'button'
	value: string
	variant?: 'text' | 'outlined' | 'contained'
	color?: 'secondary' | 'error' | 'success'
	classname?: string
	handler?: (param?: string) => void
}
