import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  AccountTreeSharpIcon,
  AppRegistrationIcon,
  BadgeIcon,
  BarChartIcon,
  EmojiEventsIcon,
  ExpandLessIcon,
  ExpandMoreIcon,
  GavelIcon,
  GroupsIcon,
  LockIcon,
  LogoutIcon,
  ManageAccountsSharpIcon,
  PeopleIcon,
  PersonIcon,
  SettingsIcon,
  SportsBasketballSharpIcon,
  SportsIcon,
  StadiumIcon,
  StarIcon,
} from '../MUI/icons/icons';
import {
  Box,
  Collapse,
  Drawer,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Divider,
} from '@mui/material';
import { useAuth } from '../../../modules/auth/hook/auth.hook';
import { UserRolesType } from '../../../modules/core/enum/user/userRolesType';

const DRAWER_WIDTH = 240;

interface NavTab {
  label: string;
  path?: string;
  icon: React.ReactNode;
  children?: NavTab[];
  disabled?: boolean;
}

const TAB_ICONS: Record<string, React.ReactNode> = {
  Jugadores: <PersonIcon />,
  Equipo: <GroupsIcon />,
  Torneo: <SportsIcon />,
  Administracion: <ManageAccountsSharpIcon />,
  AdministracionDeEquipos: <GroupsIcon />,
  Equipos: <GroupsIcon />,
  Registro: <AppRegistrationIcon />,
  Torneos: <EmojiEventsIcon />,
  Divisiones: <SportsBasketballSharpIcon />,
  Fases: <AccountTreeSharpIcon />,
  Partidos: <SportsIcon />,
  Usuarios: <PeopleIcon />,
  Configuracion: <SettingsIcon />,
  Estadisticas: <BarChartIcon />,
  CambiarPassword: <LockIcon />,
  EditarPerfil: <BadgeIcon />,
  Sanciones: <GavelIcon />,
  Puntuaciones: <StarIcon />,
  Canchas: <StadiumIcon />,
};

const CONFIGURATION_CHILDREN: NavTab[] = [
  {
    label: 'Cambiar password',
    path: '/panel/configuracion/cambiar-password',
    icon: TAB_ICONS['CambiarPassword'],
  },
  {
    label: 'Editar perfil',
    path: '/panel/configuracion/editar-perfil',
    icon: TAB_ICONS['EditarPerfil'],
  },
];

const ADMINISTRATION_CHILDREN: NavTab[] = [
  {
    label: 'Torneos',
    path: '/panel/torneos',
    icon: TAB_ICONS['Torneos'],
  },
  {
    label: 'Divisiones',
    path: '/panel/divisiones',
    icon: TAB_ICONS['Divisiones'],
  },
  {
    label: 'Fases',
    path: '/panel/fases',
    icon: TAB_ICONS['Fases'],
  },
  {
    label: 'Partidos',
    path: '/panel/partidos',
    icon: TAB_ICONS['Partidos'],
  },
  {
    label: 'Sanciones',
    path: '/panel/sanciones',
    icon: TAB_ICONS['Sanciones'],
  },
  {
    label: 'Puntuaciones',
    path: '/panel/puntuaciones',
    icon: TAB_ICONS['Puntuaciones'],
  },
  {
    label: 'Canchas',
    path: '/panel/canchas',
    icon: TAB_ICONS['Canchas'],
  },
];

const TEAM_ADMINISTRATION_CHILDREN: NavTab[] = [
  {
    label: 'Equipos',
    path: '/panel/equipos',
    icon: TAB_ICONS['Equipos'],
  },
  {
    label: 'Registro',
    path: '/panel/registro-equipos',
    icon: TAB_ICONS['Registro'],
  },
  {
    label: 'Jugadores',
    icon: TAB_ICONS['Jugadores'],
    path: '/panel/jugadores',
  },
];

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
    {
      label: 'Administracion',
      icon: TAB_ICONS['Administracion'],
      children: ADMINISTRATION_CHILDREN,
    },
    {
      label: 'Administracion de Equipos',
      icon: TAB_ICONS['AdministracionDeEquipos'],
      children: TEAM_ADMINISTRATION_CHILDREN,
    },
  ],
  [UserRolesType.Owner]: [
    {
      label: 'Administracion',
      icon: TAB_ICONS['Administracion'],
      children: ADMINISTRATION_CHILDREN,
    },
    {
      label: 'Administracion de Equipos',
      icon: TAB_ICONS['AdministracionDeEquipos'],
      children: TEAM_ADMINISTRATION_CHILDREN,
    },
    { label: 'Usuarios', path: '/panel/usuarios', icon: TAB_ICONS['Usuarios'] },
    {
      label: 'Configuracion',
      icon: TAB_ICONS['Configuracion'],
      children: CONFIGURATION_CHILDREN,
    },
  ],
  [UserRolesType.Admin]: [
    { label: 'Usuarios', path: '/panel/usuarios', icon: TAB_ICONS['Usuarios'] },
    {
      label: 'Configuracion',
      icon: TAB_ICONS['Configuracion'],
      children: CONFIGURATION_CHILDREN,
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
  const [openSections, setOpenSections] = React.useState<
    Record<string, boolean>
  >({
    Administracion: true,
    'Administracion de Equipos': true,
    Configuracion: location.pathname.startsWith('/panel/configuracion'),
  });

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
        {tabs.map(tab => {
          if (tab.children && tab.children.length > 0) {
            const isParentSelected = tab.children.some(
              child => child.path && location.pathname.startsWith(child.path)
            );
            const isOpen = openSections[tab.label] ?? false;

            return (
              <React.Fragment key={tab.label}>
                <ListItemButton
                  selected={isParentSelected}
                  onClick={() =>
                    setOpenSections(prev => ({
                      ...prev,
                      [tab.label]: !(prev[tab.label] ?? false),
                    }))
                  }
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
                  {isOpen ? <ExpandLessIcon /> : <ExpandMoreIcon />}
                </ListItemButton>

                <Collapse in={isOpen} timeout="auto" unmountOnExit>
                  <List component="div" disablePadding>
                    {tab.children.map(child => (
                      <ListItemButton
                        key={child.path ?? child.label}
                        selected={Boolean(
                          child.path && location.pathname.startsWith(child.path)
                        )}
                        onClick={() => child.path && navigate(child.path)}
                        disabled={child.disabled}
                        sx={{
                          mx: 1,
                          borderRadius: 1,
                          mb: 0.5,
                          pl: 4,
                          '&.Mui-selected': {
                            backgroundColor: 'primary.main',
                            color: 'white',
                            '& .MuiListItemIcon-root': { color: 'white' },
                            '&:hover': { backgroundColor: 'primary.dark' },
                          },
                        }}
                      >
                        <ListItemIcon sx={{ minWidth: 36 }}>
                          {child.icon}
                        </ListItemIcon>
                        <ListItemText primary={child.label} />
                      </ListItemButton>
                    ))}
                  </List>
                </Collapse>
              </React.Fragment>
            );
          }

          return (
            <ListItemButton
              key={tab.path}
              selected={Boolean(
                tab.path && location.pathname.startsWith(tab.path)
              )}
              onClick={() => tab.path && navigate(tab.path)}
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
          );
        })}
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
