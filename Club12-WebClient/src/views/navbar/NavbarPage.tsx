import { Link } from 'react-router-dom'

export const NavBarPage = () => {
	return (
		<header>
			<nav>
				<ul>
					<li>
						<Link to={'/'}>Home</Link>
					</li>
					<li>
						<Link to={'/Consulta'}>Consulta</Link>
					</li>
					<li>
						<Link to={'/login'}>Login</Link>
					</li>
					<li>
						<Link to={'/about'}>Algo mas</Link>
					</li>
				</ul>
			</nav>
		</header>
	)
}
