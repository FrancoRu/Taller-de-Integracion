import { Outlet } from 'react-router-dom'
import Header from './header/Header'
import Footer from './footer/Footer'

const Layout: React.FC = () => {
	return (
		<>
			<Header />
			<Footer />
			<Outlet />
		</>
	)
}

export default Layout
/*
<Box
				position='static'
				sx={{
					background: 'white',
					color: 'black'
				}}
			>
				<Toolbar>
					<Typography variant='h5' sx={{ fontWeight: 'bold' }}>
						Club 12
					</Typography>
					<Box sx={{ marginRight: 'auto', marginLeft: 2 }}>
						<Button component={RouterLink} to='/home' color='inherit'>
							Home
						</Button>
					</Box>
				</Toolbar>
			</Box>
*/ 