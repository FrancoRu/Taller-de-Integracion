import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  Box,
  ListItemButton,
  ListItemText,
  Menu,
  MenuItem,
} from '@mui/material';
import { ExpandLessIcon, ExpandMoreIcon } from '@/views/core/MUI/icons/icons';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import TournamentMenuItems from '@/views/home/tournaments/tournamentsMenuItems';

interface DesktopNavItemsProps {
  onCloseAnyMenu?: () => void;
}

const DesktopNavItems: React.FC<DesktopNavItemsProps> = ({
  onCloseAnyMenu,
}) => {
  const location = useLocation();
  const [anchorElInfoMenu, setAnchorElInfoMenu] = useState<null | HTMLElement>(
    null
  );
  const openInfoMenu = Boolean(anchorElInfoMenu);

  const handleOpenInfoMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorElInfoMenu(event.currentTarget);
  };

  const handleCloseInfoMenu = () => {
    setAnchorElInfoMenu(null);
  };

  const handleNavigationAndCloseMenu = (_: string) => {
    handleCloseInfoMenu();
    if (onCloseAnyMenu) {
      onCloseAnyMenu();
    }
  };

  const isSelected = (path: string) => location.pathname === path;

  return (
    <Box sx={{ display: 'flex', gap: 2 }}>
      <ListItemButton
        component={Link}
        to={RoutesNavigationViews.Home}
        selected={isSelected(RoutesNavigationViews.Home)}
        onClick={() => handleNavigationAndCloseMenu(RoutesNavigationViews.Home)}
      >
        <ListItemText primary="Inicio" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={RoutesNavigationViews.How_We_Are}
        selected={isSelected(RoutesNavigationViews.How_We_Are)}
        onClick={() =>
          handleNavigationAndCloseMenu(RoutesNavigationViews.How_We_Are)
        }
      >
        <ListItemText primary="Quiénes somos" />
      </ListItemButton>

      <ListItemButton
        id="information-button"
        aria-controls={openInfoMenu ? 'information-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={openInfoMenu ? 'true' : undefined}
        onClick={handleOpenInfoMenu}
      >
        <ListItemText primary="Información" />
        {openInfoMenu ? <ExpandLessIcon /> : <ExpandMoreIcon />}
      </ListItemButton>
      <Menu
        id="information-menu"
        MenuListProps={{
          'aria-labelledby': 'information-button',
        }}
        anchorEl={anchorElInfoMenu}
        open={openInfoMenu}
        onClose={handleCloseInfoMenu}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'left',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'left',
        }}
      >
        <Link
          to={RoutesNavigationViews.Medical_Record}
          style={{ textDecoration: 'none', color: 'inherit' }}
          onClick={() =>
            handleNavigationAndCloseMenu(RoutesNavigationViews.Medical_Record)
          }
        >
          <MenuItem selected={isSelected(RoutesNavigationViews.Medical_Record)}>
            Ficha Medica
          </MenuItem>
        </Link>
        <Link
          to={RoutesNavigationViews.Rules}
          style={{ textDecoration: 'none', color: 'inherit' }}
          onClick={() =>
            handleNavigationAndCloseMenu(RoutesNavigationViews.Rules)
          }
        >
          <MenuItem selected={isSelected(RoutesNavigationViews.Rules)}>
            Reglamento
          </MenuItem>
        </Link>
      </Menu>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Teams}`}
        selected={isSelected(`/${RoutesNavigationViews.Teams}`)}
        onClick={() =>
          handleNavigationAndCloseMenu(`/${RoutesNavigationViews.Teams}`)
        }
      >
        <ListItemText primary="Equipos" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Scorers}`}
        selected={isSelected(`/${RoutesNavigationViews.Scorers}`)}
        onClick={() =>
          handleNavigationAndCloseMenu(`/${RoutesNavigationViews.Scorers}`)
        }
      >
        <ListItemText primary="Goleadores" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Matches}`}
        selected={isSelected(`/${RoutesNavigationViews.Matches}`)}
        onClick={() =>
          handleNavigationAndCloseMenu(`/${RoutesNavigationViews.Matches}`)
        }
      >
        <ListItemText primary="Partidos" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Sanctions}`}
        selected={isSelected(`/${RoutesNavigationViews.Sanctions}`)}
        onClick={() =>
          handleNavigationAndCloseMenu(`/${RoutesNavigationViews.Sanctions}`)
        }
      >
        <ListItemText primary="Sanciones" />
      </ListItemButton>

      <TournamentMenuItems onNavigate={handleNavigationAndCloseMenu} />
    </Box>
  );
};

export default DesktopNavItems;
