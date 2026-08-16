import { useState } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Drawer,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import { MenuIcon } from '@/views/core/MUI/icons/icons';
import DesktopNavItems from './desktop';
import MobileNavItems from './mobile';

const NavMenu = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const [mobileOpen, setMobileOpen] = useState(false);

  const toggleDrawer = () => setMobileOpen(!mobileOpen);
  const closeDrawer = () => setMobileOpen(false);

  return (
    <AppBar position="static">
      <Toolbar sx={{ display: 'flex', justifyContent: 'space-between' }}>
        {isMobile ? (
          <>
            <IconButton
              color="inherit"
              edge="start"
              onClick={toggleDrawer}
              aria-label="menu"
            >
              <MenuIcon />
            </IconButton>
            <Drawer
              anchor="left"
              open={mobileOpen}
              onClose={toggleDrawer}
              ModalProps={{ keepMounted: true }}
              sx={{ '& .MuiDrawer-paper': { width: 250 } }}
            >
              <MobileNavItems onCloseDrawer={closeDrawer} />
            </Drawer>
          </>
        ) : (
          <DesktopNavItems />
        )}

        <Typography variant="h6" component="div">
          Mi Aplicación
        </Typography>
      </Toolbar>
    </AppBar>
  );
};

export default NavMenu;
