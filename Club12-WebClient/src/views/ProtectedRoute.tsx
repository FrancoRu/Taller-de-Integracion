import { Outlet } from 'react-router-dom'

export const ProtectedRoute = () => {
	// if (!isAuthenticated) return <Navigate to="/login" replace></Navigate>

	// if (firstCharge) getProjects()

	return <Outlet />
}

export default ProtectedRoute
