import { useEffect, useState } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  IconButton,
  Drawer,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import DesktopNavItems from './desktop';
import MobileNavItems from './mobile';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';

const NavMenu = () => {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const [mobileOpen, setMobileOpen] = useState(false);
  const { getAllTournamentsByFilter } = useTournament();

  const toggleDrawer = () => setMobileOpen(!mobileOpen);
  const closeDrawer = () => setMobileOpen(false);

  useEffect(() => {
    (async () => {
      await getAllTournamentsByFilter({});
    })();
  }, []);

  return (
    <AppBar position="static">
      <Toolbar>
        <Typography variant="h6" sx={{ flexGrow: 1 }}>
          Mi Aplicación
        </Typography>

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
      </Toolbar>
    </AppBar>
  );
};

export default NavMenu;
