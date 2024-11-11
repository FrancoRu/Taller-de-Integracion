import React from 'react'
import { Link } from 'react-router-dom'
import '../../styles/navbar/navbar.css'
import { Routes } from '../../types/types.d'
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
						<li className='li-navbar'>
							<Link to={Routes.HOME}>Inicio</Link>
						</li>
						<li className='li-navbar'>
							<Link to={Routes.CAMPEONATO}>Campeonato</Link>
						</li>
						<li className='li-navbar'>
							<Link to={Routes.FEMENINO}>Femenino</Link>
						</li>
						<li className='li-navbar'>
							<Link to={Routes.LA_PREVIA}>La Previa</Link>
						</li>
						<li className='li-navbar'>
							<Link to={Routes.COPA_12}>Copa 12</Link>
						</li>
					</ul>
				</nav>
			</header>
		</div>
	)
}
