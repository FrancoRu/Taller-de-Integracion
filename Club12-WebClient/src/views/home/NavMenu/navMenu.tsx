import { useState } from 'react';
import {
  AppBar,
  Toolbar,
  IconButton,
  Drawer,
  Box,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import { Link } from 'react-router-dom';
import { MenuIcon } from '@/views/core/MUI/icons/icons';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { LOGO_BACKGROUND_COLOR } from '@/theme';
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

        <Box
          component={Link}
          to={RoutesNavigationViews.Home}
          sx={{
            display: 'flex',
            alignItems: 'center',
            bgcolor: LOGO_BACKGROUND_COLOR,
            borderRadius: 1.5,
            p: 0.5,
          }}
        >
          <Box
            component="img"
            src="/assets/logo-club12.png"
            alt="Club 12"
            sx={{ height: 40, width: 'auto', display: 'block' }}
          />
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default NavMenu;
