import React from 'react'
import { Link } from 'react-router-dom'
import '../../styles/navbar/navbar.css'
/**
 *
 * @param prop
 * @returns
 */

/**
 *
 * TODO
 * To render the navigation bar, the data must be passed to it in an object format:
 * type props = {
 * 	links: string
 * 	labels: string
 * }
 * where links will be the desired relative path (clarification: It should not have the first bar since
 * this will be assigned automatically) and labels, which is what will be shown to users, for example
 * {
 * 	link:'',
 * 	label: 'Home'
 * }
 * will show the home button for the relative path '/'
 */
export const NavBarPage: React.FC = () => {
	return (
		<div className='container-header'>
			<header>
				<nav className='nav-navbar'>
					<ul className='ul-navbar'>
						<li>
							<Link to={'/teams'}>Equipos</Link>
						</li>
						<li>
							<Link to={'/players'}>Jugadores</Link>
						</li>
						<li>
							<Link to={'/matches'}>Partidos</Link>
						</li>
						<li>
							<Link to={'/tournaments'}>Torneos</Link>
						</li>
						<li>
							<Link to={'/statistics'}>Estadísticas</Link>
						</li>
					</ul>
				</nav>
			</header>
		</div>
	)
}
