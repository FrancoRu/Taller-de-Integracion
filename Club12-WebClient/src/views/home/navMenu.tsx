import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  MenuItem,
  Menu,
  IconButton,
  Drawer,
  List,
  ListItem,
  ListItemText,
  useTheme,
  useMediaQuery,
  Divider,
  Collapse,
} from '@mui/material';
import { orange, grey } from '@mui/material/colors';
import MenuIcon from '@mui/icons-material/Menu';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';

const NavMenu = () => {
  const theme = useTheme();
  const location = useLocation();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));
  const [mobileOpen, setMobileOpen] = React.useState(false);
  const [campeonatoAnchorEl, setCampeonatoAnchorEl] =
    React.useState<null | HTMLElement>(null);
  const [informacionAnchorEl, setInformacionAnchorEl] =
    React.useState<null | HTMLElement>(null);
  const [mobileInformacionOpen, setMobileInformacionOpen] =
    React.useState(false);
  const [mobileCampeonatoOpen, setMobileCampeonatoOpen] = React.useState(false);

  const handleCampeonatoClick = (event: React.MouseEvent<HTMLElement>) => {
    if (isMobile) {
      setMobileCampeonatoOpen(!mobileCampeonatoOpen);
    } else {
      setCampeonatoAnchorEl(event.currentTarget);
    }
  };

  const handleInformacionClick = (event: React.MouseEvent<HTMLElement>) => {
    if (isMobile) {
      setMobileInformacionOpen(!mobileInformacionOpen);
    } else {
      setInformacionAnchorEl(event.currentTarget);
    }
  };

  const handleMenuClose = () => {
    setCampeonatoAnchorEl(null);
    setInformacionAnchorEl(null);
  };

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleMobileMenuClick = () => {
    setMobileOpen(false);
    setMobileInformacionOpen(false);
    setMobileCampeonatoOpen(false);
  };

  const isSelected = (path: string) => {
    return location.pathname === path;
  };

  const mobileDrawer = (
    <Box sx={{ width: 250, bgcolor: orange[500], height: '100%' }}>
      <Box
        sx={{
          p: 2,
          display: 'flex',
          alignItems: 'center',
          bgcolor: orange[600],
        }}
      >
        <img
          src="/assets/images/club-12-basquet.webp"
          alt="Club12 Logo"
          style={{
            height: '40px',
            marginRight: '12px',
            borderRadius: '4px',
          }}
        />
        <Typography variant="h6" sx={{ fontWeight: 'bold', color: grey[900] }}>
          CLUB12 - APP
        </Typography>
      </Box>
      <Divider sx={{ bgcolor: orange[600] }} />
      <List>
        <ListItem
          button
          component={Link}
          to="/"
          onClick={handleMobileMenuClick}
          selected={isSelected('/')}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="INICIO" sx={{ color: grey[900] }} />
        </ListItem>
        <ListItem
          button
          component={Link}
          to="/quienes-somos"
          onClick={handleMobileMenuClick}
          selected={isSelected('/quienes-somos')}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="¿QUIENES SOMOS?" sx={{ color: grey[900] }} />
        </ListItem>

        {/* Información Submenu */}
        <ListItem
          button
          onClick={handleInformacionClick}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="INFORMACIÓN" sx={{ color: grey[900] }} />
          {mobileInformacionOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItem>
        <Collapse in={mobileInformacionOpen} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            <ListItem
              button
              component={Link}
              to="/ficha-medica"
              onClick={handleMobileMenuClick}
              selected={isSelected('/ficha-medica')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="Ficha Médica" sx={{ color: grey[900] }} />
            </ListItem>
            <ListItem
              button
              component={Link}
              to="/reglamento"
              onClick={handleMobileMenuClick}
              selected={isSelected('/reglamento')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="Reglamento" sx={{ color: grey[900] }} />
            </ListItem>
          </List>
        </Collapse>

        {/* Campeonato Submenu */}
        <ListItem
          button
          onClick={handleCampeonatoClick}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="CAMPEONATO" sx={{ color: grey[900] }} />
          {mobileCampeonatoOpen ? <ExpandLess /> : <ExpandMore />}
        </ListItem>
        <Collapse in={mobileCampeonatoOpen} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            <ListItem
              button
              component={Link}
              to="/zona-a"
              onClick={handleMobileMenuClick}
              selected={isSelected('/zona-a')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="ZONA A" sx={{ color: grey[900] }} />
            </ListItem>
            <ListItem
              button
              component={Link}
              to="/zona-b"
              onClick={handleMobileMenuClick}
              selected={isSelected('/zona-b')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="ZONA B" sx={{ color: grey[900] }} />
            </ListItem>
            <ListItem
              button
              component={Link}
              to="/zona-c"
              onClick={handleMobileMenuClick}
              selected={isSelected('/zona-c')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="ZONA C" sx={{ color: grey[900] }} />
            </ListItem>
            <ListItem
              button
              component={Link}
              to="/zona-d"
              onClick={handleMobileMenuClick}
              selected={isSelected('/zona-d')}
              sx={{
                pl: 4,
                '&.Mui-selected': {
                  backgroundColor: orange[700],
                  '&:hover': {
                    backgroundColor: orange[600],
                  },
                },
              }}
            >
              <ListItemText primary="ZONA D" sx={{ color: grey[900] }} />
            </ListItem>
          </List>
        </Collapse>

        <ListItem
          button
          component={Link}
          to="/copa-c12"
          onClick={handleMobileMenuClick}
          selected={isSelected('/copa-c12')}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="COPA C12" sx={{ color: grey[900] }} />
        </ListItem>
        <ListItem
          button
          component={Link}
          to="/femenino"
          onClick={handleMobileMenuClick}
          selected={isSelected('/femenino')}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="FEMENINO" sx={{ color: grey[900] }} />
        </ListItem>
        <ListItem
          button
          component={Link}
          to="/la-previa"
          onClick={handleMobileMenuClick}
          selected={isSelected('/la-previa')}
          sx={{
            '&.Mui-selected': {
              backgroundColor: orange[700],
              '&:hover': {
                backgroundColor: orange[600],
              },
            },
          }}
        >
          <ListItemText primary="LA PREVIA" sx={{ color: grey[900] }} />
        </ListItem>
      </List>
    </Box>
  );

  return (
    <AppBar position="sticky" sx={{ backgroundColor: orange[500] }}>
      <Toolbar sx={{ justifyContent: 'space-between', gap: 2 }}>
        {/* Left side - Logo and Title (only visible on desktop) */}
        <Box sx={{ display: 'flex', alignItems: 'center', flexShrink: 0 }}>
          {isMobile ? (
            <IconButton
              color="inherit"
              aria-label="open drawer"
              edge="start"
              onClick={handleDrawerToggle}
              sx={{ mr: 2, color: grey[900] }}
            >
              <MenuIcon />
            </IconButton>
          ) : (
            <>
              <img
                src="/assets/images/club-12-basquet.webp"
                alt="Club12 Logo"
                style={{
                  height: '40px',
                  marginRight: '12px',
                  borderRadius: '4px',
                }}
              />
              <Typography
                variant="h6"
                sx={{ fontWeight: 'bold', color: grey[900] }}
              >
                CLUB12 - APP
              </Typography>
            </>
          )}
        </Box>

        {/* Center - Navigation Tabs */}
        {!isMobile && (
          <Box
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 2,
              position: 'absolute',
              left: '50%',
              transform: 'translateX(-50%)',
            }}
          >
            <Button
              component={Link}
              to="/"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: isSelected('/') ? orange[700] : 'transparent',
                '&:hover': {
                  backgroundColor: isSelected('/') ? orange[600] : orange[400],
                },
              }}
            >
              INICIO
            </Button>

            <Button
              component={Link}
              to="/quienes-somos"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: isSelected('/quienes-somos')
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: isSelected('/quienes-somos')
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              ¿QUIENES SOMOS?
            </Button>

            <Button
              onClick={handleInformacionClick}
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: ['/ficha-medica', '/reglamento'].some(path =>
                  isSelected(path)
                )
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: ['/ficha-medica', '/reglamento'].some(path =>
                    isSelected(path)
                  )
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              INFORMACIÓN
            </Button>

            <Button
              onClick={handleCampeonatoClick}
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: [
                  '/zona-a',
                  '/zona-b',
                  '/zona-c',
                  '/zona-d',
                ].some(path => isSelected(path))
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: [
                    '/zona-a',
                    '/zona-b',
                    '/zona-c',
                    '/zona-d',
                  ].some(path => isSelected(path))
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              CAMPEONATO
            </Button>

            <Button
              component={Link}
              to="/copa-c12"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: isSelected('/copa-c12')
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: isSelected('/copa-c12')
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              COPA C12
            </Button>

            <Button
              component={Link}
              to="/femenino"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: isSelected('/femenino')
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: isSelected('/femenino')
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              FEMENINO
            </Button>

            <Button
              component={Link}
              to="/la-previa"
              sx={{
                color: grey[900],
                fontWeight: 'bold',
                backgroundColor: isSelected('/la-previa')
                  ? orange[700]
                  : 'transparent',
                '&:hover': {
                  backgroundColor: isSelected('/la-previa')
                    ? orange[600]
                    : orange[400],
                },
              }}
            >
              LA PREVIA
            </Button>
          </Box>
        )}

        {/* Right side - Empty space to balance the layout */}
        <Box sx={{ flexShrink: 0, width: isMobile ? 'auto' : '200px' }} />

        <Menu
          anchorEl={informacionAnchorEl}
          open={Boolean(informacionAnchorEl)}
          onClose={handleMenuClose}
        >
          <MenuItem
            component={Link}
            to="/ficha-medica"
            onClick={handleMenuClose}
            selected={isSelected('/ficha-medica')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            Ficha Médica
          </MenuItem>
          <MenuItem
            component={Link}
            to="/reglamento"
            onClick={handleMenuClose}
            selected={isSelected('/reglamento')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            Reglamento
          </MenuItem>
        </Menu>

        <Menu
          anchorEl={campeonatoAnchorEl}
          open={Boolean(campeonatoAnchorEl)}
          onClose={handleMenuClose}
        >
          <MenuItem
            component={Link}
            to="/zona-a"
            onClick={handleMenuClose}
            selected={isSelected('/zona-a')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            ZONA A
          </MenuItem>
          <MenuItem
            component={Link}
            to="/zona-b"
            onClick={handleMenuClose}
            selected={isSelected('/zona-b')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            ZONA B
          </MenuItem>
          <MenuItem
            component={Link}
            to="/zona-c"
            onClick={handleMenuClose}
            selected={isSelected('/zona-c')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            ZONA C
          </MenuItem>
          <MenuItem
            component={Link}
            to="/zona-d"
            onClick={handleMenuClose}
            selected={isSelected('/zona-d')}
            sx={{
              '&.Mui-selected': {
                backgroundColor: orange[700],
                '&:hover': {
                  backgroundColor: orange[600],
                },
              },
            }}
          >
            ZONA D
          </MenuItem>
        </Menu>
      </Toolbar>

      <Drawer
        variant="temporary"
        anchor="left"
        open={mobileOpen}
        onClose={handleDrawerToggle}
        ModalProps={{
          keepMounted: true,
        }}
      >
        {mobileDrawer}
      </Drawer>
    </AppBar>
  );
};

export default NavMenu;
