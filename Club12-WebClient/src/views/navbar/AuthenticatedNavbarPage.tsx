import React from 'react'
import { Link } from 'react-router-dom'
type props = {
	text: string
	link: string
}
interface AuthenticatedNavbarPageProps {
	value: string
	text: string
}
const CRUD: props[] = [
	{
		text: 'Agregar',
		link: 'add'
	},
	{
		text: 'Modificar',
		link: 'modify'
	},
	{
		text: 'Eliminar',
		link: 'delete'
	},
	{
		text: 'Buscar',
		link: 'find'
	}
]

export const AuthenticatedNavbarPage: React.FC<
	AuthenticatedNavbarPageProps
> = ({ value, text }) => {
	return (
		<ul className='ul-navbar-submenu'>
			{CRUD.map((element: props) => (
				<li className='li-navbar-submenu' key={element.link}>
					<Link to={`${value}/${element.link}`}>{`${element.text} ${text}`}</Link>
				</li>
			))}
		</ul>
	)
}
