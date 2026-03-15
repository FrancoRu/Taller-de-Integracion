import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Box,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
} from '@mui/material';
import SportsIcon from '@mui/icons-material/Sports';
import GroupsIcon from '@mui/icons-material/Groups';
import PersonIcon from '@mui/icons-material/Person';
import PeopleIcon from '@mui/icons-material/People';
import SettingsIcon from '@mui/icons-material/Settings';
import BarChartIcon from '@mui/icons-material/BarChart';
import LogoutIcon from '@mui/icons-material/Logout';
import { useAuth } from '../../../modules/auth/hook/auth.hook';
import { UserRolesType } from '../../../modules/core/enum/user/userRolesType';

const DRAWER_WIDTH = 240;

interface NavTab {
  label: string;
  path: string;
  icon: React.ReactNode;
}

const TAB_ICONS: Record<string, React.ReactNode> = {
  Jugadores: <PersonIcon />,
  Equipo: <GroupsIcon />,
  Torneo: <SportsIcon />,
  Equipos: <GroupsIcon />,
  Torneos: <SportsIcon />,
  Usuarios: <PeopleIcon />,
  Configuracion: <SettingsIcon />,
  Estadisticas: <BarChartIcon />,
};

const TABS_BY_ROLE: Record<UserRolesType, NavTab[]> = {
  [UserRolesType.TeamManager]: [
    {
      label: 'Jugadores',
      path: '/panel/jugadores',
      icon: TAB_ICONS['Jugadores'],
    },
    { label: 'Equipo', path: '/panel/equipo', icon: TAB_ICONS['Equipo'] },
  ],
  [UserRolesType.TournamentManager]: [
    { label: 'Torneo', path: '/panel/torneo', icon: TAB_ICONS['Torneo'] },
    { label: 'Equipos', path: '/panel/equipos', icon: TAB_ICONS['Equipos'] },
  ],
  [UserRolesType.Owner]: [
    { label: 'Torneos', path: '/panel/torneos', icon: TAB_ICONS['Torneos'] },
    { label: 'Equipos', path: '/panel/equipos', icon: TAB_ICONS['Equipos'] },
    {
      label: 'Jugadores',
      path: '/panel/jugadores',
      icon: TAB_ICONS['Jugadores'],
    },
    { label: 'Usuarios', path: '/panel/usuarios', icon: TAB_ICONS['Usuarios'] },
    {
      label: 'Configuracion',
      path: '/panel/configuracion',
      icon: TAB_ICONS['Configuracion'],
    },
  ],
  [UserRolesType.Admin]: [
    { label: 'Usuarios', path: '/panel/usuarios', icon: TAB_ICONS['Usuarios'] },
    {
      label: 'Configuracion',
      path: '/panel/configuracion',
      icon: TAB_ICONS['Configuracion'],
    },
    {
      label: 'Estadisticas',
      path: '/panel/estadisticas',
      icon: TAB_ICONS['Estadisticas'],
    },
  ],
  [UserRolesType.Guest]: [],
};

const SidebarLayout: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const { role, logOut } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const tabs = TABS_BY_ROLE[role] ?? [];

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ px: 2 }}>
        <Typography variant="h6" noWrap color="primary" fontWeight={700}>
          Club 12
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ flexGrow: 1, pt: 1 }}>
        {tabs.map(tab => (
          <ListItemButton
            key={tab.path}
            selected={location.pathname.startsWith(tab.path)}
            onClick={() => navigate(tab.path)}
            sx={{
              mx: 1,
              borderRadius: 1,
              mb: 0.5,
              '&.Mui-selected': {
                backgroundColor: 'primary.main',
                color: 'white',
                '& .MuiListItemIcon-root': { color: 'white' },
                '&:hover': { backgroundColor: 'primary.dark' },
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 36 }}>{tab.icon}</ListItemIcon>
            <ListItemText primary={tab.label} />
          </ListItemButton>
        ))}
      </List>
      <Divider />
      <List>
        <ListItemButton onClick={logOut} sx={{ mx: 1, borderRadius: 1, mb: 1 }}>
          <ListItemIcon sx={{ minWidth: 36 }}>
            <LogoutIcon />
          </ListItemIcon>
          <ListItemText primary="Cerrar sesión" />
        </ListItemButton>
      </List>
    </Box>
  );

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Drawer
        variant="permanent"
        sx={{
          width: DRAWER_WIDTH,
          flexShrink: 0,
          '& .MuiDrawer-paper': {
            width: DRAWER_WIDTH,
            boxSizing: 'border-box',
            backgroundColor: 'background.paper',
            borderRight: '1px solid',
            borderColor: 'divider',
          },
        }}
      >
        {drawerContent}
      </Drawer>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          backgroundColor: 'background.default',
          minHeight: '100vh',
        }}
      >
        {children}
      </Box>
    </Box>
  );
};

export default SidebarLayout;
