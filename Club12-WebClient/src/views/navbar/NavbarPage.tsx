import React, { useState } from 'react'
import { Link } from 'react-router-dom'
import '../../styles/navbar/navbar.css'
import { AuthenticatedNavbarPage } from './AuthenticatedNavbarPage'
import { useAuth } from '../../hooks/auth/useAuth'
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
	const { isAuthenticated } = useAuth()
	const [showSubMenu, setShowSubMenu] = useState<boolean>(false)
	const toggleSubMenu = () => {
		setShowSubMenu(!showSubMenu)
	}
	return (
		<div className='container-header'>
			<header>
				<nav className='nav-navbar'>
					<ul className='ul-navbar'>
						<li className='li-navbar' onClick={toggleSubMenu}>
							{isAuthenticated ? (
								<Link to={'/teams'}>Equipos</Link>
							) : (
								<>
									<p>Equipos</p>
									<span>{showSubMenu ? '▲' : '▼'}</span>
									{showSubMenu && (
										<AuthenticatedNavbarPage value='teams' text='Equipos' />
									)}
								</>
							)}
						</li>
						<li className='li-navbar'>
							<Link to={'/players'}>Jugadores</Link>
						</li>
						<li className='li-navbar'>
							<Link to={'/matches'}>Partidos</Link>
						</li>
						<li className='li-navbar'>
							<Link to={'/tournaments'}>Torneos</Link>
						</li>
						<li className='li-navbar'>
							<Link to={'/statistics'}>Estadísticas</Link>
						</li>
					</ul>
				</nav>
			</header>
		</div>
	)
}
