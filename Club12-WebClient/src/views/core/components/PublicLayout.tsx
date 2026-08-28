import { Box } from '@mui/material';
import { Outlet } from 'react-router-dom';
import NavMenu from '@/views/home/NavMenu/navMenu';
import Footer from '@/views/home/Footer/Footer';

/**
 * Layout route for the public (unauthenticated) site: header + footer chrome
 * around the matched page. Routes that must render chrome-free (login, 404)
 * live outside this layout in App.
 */
export default function PublicLayout() {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <NavMenu />
      <Box component="main" sx={{ flex: 1 }}>
        <Outlet />
      </Box>
      <Footer />
    </Box>
  );
}
