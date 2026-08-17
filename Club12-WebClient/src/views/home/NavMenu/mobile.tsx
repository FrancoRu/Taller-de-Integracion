import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Box, List, ListItemButton, ListItemText, Collapse } from '@mui/material';
import { ExpandLessIcon, ExpandMoreIcon } from '@/views/core/MUI/icons/icons';
import { RoutesNavigationViews } from '@/views/core/routes-const';

interface MobileNavItemsProps {
  onCloseDrawer: () => void;
}

const MobileNavItems: React.FC<MobileNavItemsProps> = ({ onCloseDrawer }) => {
  const location = useLocation();
  const [openInfoCollapse, setOpenInfoCollapse] = useState(false);

  const toggleInfoCollapse = () => {
    setOpenInfoCollapse(!openInfoCollapse);
  };

  const handleNavigationAndCloseDrawer = (_: string) => {
    onCloseDrawer();
  };

  const isSelected = (path: string) => location.pathname === path;

  return (
    <Box component="nav">
    <List>
      <ListItemButton
        component={Link}
        to={RoutesNavigationViews.Home}
        selected={isSelected(RoutesNavigationViews.Home)}
        onClick={() =>
          handleNavigationAndCloseDrawer(RoutesNavigationViews.Home)
        }
      >
        <ListItemText primary="Inicio" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={RoutesNavigationViews.How_We_Are}
        selected={isSelected(RoutesNavigationViews.How_We_Are)}
        onClick={() =>
          handleNavigationAndCloseDrawer(RoutesNavigationViews.How_We_Are)
        }
      >
        <ListItemText primary="Quiénes somos" />
      </ListItemButton>

      <ListItemButton onClick={toggleInfoCollapse}>
        <ListItemText primary="Información" />
        {openInfoCollapse ? <ExpandLessIcon /> : <ExpandMoreIcon />}
      </ListItemButton>
      <Collapse in={openInfoCollapse} timeout="auto" unmountOnExit>
        <List component="div" disablePadding>
          <ListItemButton
            component={Link}
            to={RoutesNavigationViews.Medical_Record}
            selected={isSelected(RoutesNavigationViews.Medical_Record)}
            sx={{ pl: 4 }}
            onClick={() =>
              handleNavigationAndCloseDrawer(
                RoutesNavigationViews.Medical_Record
              )
            }
          >
            <ListItemText primary="Ficha Medica" />
          </ListItemButton>
          <ListItemButton
            component={Link}
            to={RoutesNavigationViews.Rules}
            selected={isSelected(RoutesNavigationViews.Rules)}
            sx={{ pl: 4 }}
            onClick={() =>
              handleNavigationAndCloseDrawer(RoutesNavigationViews.Rules)
            }
          >
            <ListItemText primary="Reglamento" />
          </ListItemButton>
        </List>
      </Collapse>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Tournaments}`}
        selected={location.pathname.startsWith('/torneos')}
        onClick={() =>
          handleNavigationAndCloseDrawer(`/${RoutesNavigationViews.Tournaments}`)
        }
      >
        <ListItemText primary="Torneos" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Sanctions}`}
        selected={isSelected(`/${RoutesNavigationViews.Sanctions}`)}
        onClick={() =>
          handleNavigationAndCloseDrawer(`/${RoutesNavigationViews.Sanctions}`)
        }
      >
        <ListItemText primary="Sanciones" />
      </ListItemButton>

      <ListItemButton
        component={Link}
        to={`/${RoutesNavigationViews.Blog}`}
        selected={isSelected(`/${RoutesNavigationViews.Blog}`)}
        onClick={() =>
          handleNavigationAndCloseDrawer(`/${RoutesNavigationViews.Blog}`)
        }
      >
        <ListItemText primary="Novedades" />
      </ListItemButton>
    </List>
    </Box>
  );
};

export default MobileNavItems;
