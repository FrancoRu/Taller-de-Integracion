import { createTheme } from '@mui/material'

const theme = createTheme({
	palette: {
		primary: {
			main: '#FF7518'
		},
		secondary: {
			main: '#F54703'
		},
		background: {
			default: '#FFFFFF'
		}
	},	
    breakpoints: {
        values: {
            xs: 0,
            sm: 600,
            md: 980,
            lg: 1200,
            xl: 1536,
        },
    },
})

export default theme

// PALETA COLORES WEB
// https://coolors.co/palette/464545-2f2f2f-1b1b1b-f54703-ff7518
