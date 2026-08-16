import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  AccountTreeSharpIcon,
  AppRegistrationIcon,
  ArticleIcon,
  AutoAwesomeIcon,
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
  ShieldIcon,
  SportsBasketballSharpIcon,
  SportsIcon,
  StadiumIcon,
  StarIcon,
} from '@/views/core/MUI/icons/icons';
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
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';

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
  Equipos: <ShieldIcon />,
  Registro: <AppRegistrationIcon />,
  Torneos: <EmojiEventsIcon />,
  AsistenteDeTorneo: <AutoAwesomeIcon />,
  Divisiones: <SportsBasketballSharpIcon />,
  Fases: <AccountTreeSharpIcon />,
  Partidos: <SportsIcon />,
  Usuarios: <PeopleIcon />,
  Blog: <ArticleIcon />,
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
    path: APP_ROUTES.panelChangePassword,
    icon: TAB_ICONS['CambiarPassword'],
  },
  {
    label: 'Editar perfil',
    path: APP_ROUTES.panelEditProfile,
    icon: TAB_ICONS['EditarPerfil'],
  },
];

const ADMINISTRATION_CHILDREN: NavTab[] = [
  {
    label: 'Torneos',
    path: APP_ROUTES.panelTournaments,
    icon: TAB_ICONS['Torneos'],
  },
  {
    label: 'Asistente de torneo',
    path: APP_ROUTES.panelTournamentWizard,
    icon: TAB_ICONS['AsistenteDeTorneo'],
  },
  {
    label: 'Divisiones',
    path: APP_ROUTES.panelDivisions,
    icon: TAB_ICONS['Divisiones'],
  },
  {
    label: 'Fases',
    path: APP_ROUTES.panelStages,
    icon: TAB_ICONS['Fases'],
  },
  {
    label: 'Partidos',
    path: APP_ROUTES.panelMatches,
    icon: TAB_ICONS['Partidos'],
  },
  {
    label: 'Sanciones',
    path: APP_ROUTES.panelSanctions,
    icon: TAB_ICONS['Sanciones'],
  },
  {
    label: 'Puntuaciones',
    path: APP_ROUTES.panelScorers,
    icon: TAB_ICONS['Puntuaciones'],
  },
  {
    label: 'Canchas',
    path: APP_ROUTES.panelVenues,
    icon: TAB_ICONS['Canchas'],
  },
];

const TEAM_ADMINISTRATION_CHILDREN: NavTab[] = [
  {
    label: 'Equipos',
    path: APP_ROUTES.panelTeams,
    icon: TAB_ICONS['Equipos'],
  },
  {
    label: 'Registro',
    path: APP_ROUTES.panelTeamRegister,
    icon: TAB_ICONS['Registro'],
  },
  {
    label: 'Jugadores',
    icon: TAB_ICONS['Jugadores'],
    path: APP_ROUTES.panelPlayers,
  },
];

const TABS_BY_ROLE: Record<UserRolesType, NavTab[]> = {
  [UserRolesType.TeamManager]: [
    {
      label: 'Jugadores',
      path: APP_ROUTES.panelPlayers,
      icon: TAB_ICONS['Jugadores'],
    },
    { label: 'Equipo', path: APP_ROUTES.panelTeam, icon: TAB_ICONS['Equipo'] },
  ],
  [UserRolesType.TournamentManager]: [
    {
      label: 'Administracion',
      icon: TAB_ICONS['Administracion'],
      children: ADMINISTRATION_CHILDREN,
    },
    {
      label: 'Gestion de Equipos',
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
      label: 'Gestion de Equipos',
      icon: TAB_ICONS['AdministracionDeEquipos'],
      children: TEAM_ADMINISTRATION_CHILDREN,
    },
    { label: 'Usuarios', path: APP_ROUTES.panelUsers, icon: TAB_ICONS['Usuarios'] },
    { label: 'Blog', path: APP_ROUTES.panelBlog, icon: TAB_ICONS['Blog'] },
    {
      label: 'Configuracion',
      icon: TAB_ICONS['Configuracion'],
      children: CONFIGURATION_CHILDREN,
    },
  ],
  [UserRolesType.Admin]: [
    { label: 'Usuarios', path: APP_ROUTES.panelUsers, icon: TAB_ICONS['Usuarios'] },
    { label: 'Blog', path: APP_ROUTES.panelBlog, icon: TAB_ICONS['Blog'] },
    {
      label: 'Configuracion',
      icon: TAB_ICONS['Configuracion'],
      children: CONFIGURATION_CHILDREN,
    },
    {
      label: 'Estadisticas',
      path: APP_ROUTES.panelStatistics,
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
    'Gestion de Equipos': true,
    Configuracion: location.pathname.startsWith(APP_ROUTES.panelSettings),
  });

  const tabs = TABS_BY_ROLE[role] ?? [];

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ px: 2 }}>
        <Typography
          variant="h6"
          component="div"
          noWrap
          color="primary"
          fontWeight={700}
        >
          Club 12
        </Typography>
      </Toolbar>
      <Divider />
      <Box component="nav" sx={{ flexGrow: 1 }}>
      <List sx={{ pt: 1 }}>
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
      </Box>
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
