// src/components/MobileNavItems.tsx
import React, { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { List, ListItemButton, ListItemText, Collapse } from '@mui/material';
import ExpandLess from '@mui/icons-material/ExpandLess';
import ExpandMore from '@mui/icons-material/ExpandMore';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import TournamentMenuItems from '../tournaments/tournamentsMenuItems';

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
    onCloseDrawer(); // Cierra el drawer
  };

  const isSelected = (path: string) => location.pathname === path;

  return (
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
        {openInfoCollapse ? <ExpandLess /> : <ExpandMore />}
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

      <TournamentMenuItems onNavigate={handleNavigationAndCloseDrawer} />
    </List>
  );
};

export default MobileNavItems;
