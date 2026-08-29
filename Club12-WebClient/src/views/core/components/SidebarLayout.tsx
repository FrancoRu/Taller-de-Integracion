import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  AppRegistrationIcon,
  ArticleIcon,
  BadgeIcon,
  BarChartIcon,
  EmojiEventsIcon,
  ExpandLessIcon,
  ExpandMoreIcon,
  GavelIcon,
  GroupsIcon,
  HistoryIcon,
  LockIcon,
  LogoutIcon,
  ManageAccountsSharpIcon,
  MenuIcon,
  PeopleIcon,
  PersonIcon,
  SettingsIcon,
  ShieldIcon,
  SportsIcon,
  StadiumIcon,
  StarIcon,
  StorageIcon,
} from '@/views/core/MUI/icons/icons';
import {
  AppBar,
  Box,
  Collapse,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Divider,
} from '@mui/material';
import { useAuth } from '@/modules/auth/hook/auth.hook';
import { UserRolesType } from '@/modules/core/enum/user/userRolesType';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { LOGO_BACKGROUND_COLOR } from '@/theme';

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
  Usuarios: <PeopleIcon />,
  Blog: <ArticleIcon />,
  Configuracion: <SettingsIcon />,
  Estadisticas: <BarChartIcon />,
  Auditoria: <HistoryIcon />,
  AdministracionDeDatos: <StorageIcon />,
  CambiarPassword: <LockIcon />,
  EditarPerfil: <BadgeIcon />,
  Sanciones: <GavelIcon />,
  Puntuaciones: <StarIcon />,
  Canchas: <StadiumIcon />,
};

const CONFIGURATION_CHILDREN: NavTab[] = [
  {
    label: 'Cambiar contraseña',
    path: APP_ROUTES.panelChangePassword,
    icon: TAB_ICONS['CambiarPassword'],
  },
  {
    label: 'Editar perfil',
    path: APP_ROUTES.panelEditProfile,
    icon: TAB_ICONS['EditarPerfil'],
  },
];

/**
 * "Competición" groups the day-to-day league pages Admin and Owner both reach
 * (mirrors the Admin/Owner backend policy on Tournament/PlayerSanction/Venue
 * controllers). Puntuaciones is Owner-only, so it lives in the Owner variant
 * below — Admin's sidebar never links to a page that bounces to /forbidden.
 *
 * HU-26: Divisiones, Fases and Partidos are not standalone entries — they are
 * managed from within a tournament (the tournament detail drills into its
 * divisions, stages and matches). HU-27: the tournament wizard is reached from
 * the "Nuevo torneo" button on the tournaments page, not the sidebar. Those
 * routes still exist and stay reachable from those flows.
 */
const COMPETITION_CHILDREN: NavTab[] = [
  {
    label: 'Torneos',
    path: APP_ROUTES.panelTournaments,
    icon: TAB_ICONS['Torneos'],
  },
  {
    label: 'Sanciones',
    path: APP_ROUTES.panelSanctions,
    icon: TAB_ICONS['Sanciones'],
  },
  {
    label: 'Canchas',
    path: APP_ROUTES.panelVenues,
    icon: TAB_ICONS['Canchas'],
  },
];

const COMPETITION_CHILDREN_OWNER: NavTab[] = [
  ...COMPETITION_CHILDREN,
  {
    label: 'Puntuaciones',
    path: APP_ROUTES.panelScorers,
    icon: TAB_ICONS['Puntuaciones'],
  },
];

const TEAM_CHILDREN: NavTab[] = [
  {
    label: 'Equipos',
    path: APP_ROUTES.panelTeams,
    icon: TAB_ICONS['Equipos'],
  },
  {
    label: 'Inscripción de equipos',
    path: APP_ROUTES.panelTeamRegister,
    icon: TAB_ICONS['Registro'],
  },
  {
    label: 'Jugadores',
    icon: TAB_ICONS['Jugadores'],
    path: APP_ROUTES.panelPlayers,
  },
];

// Low-frequency "Sistema" pages grouped together so the important, everyday
// entries stay at the top of the sidebar instead of being buried among them.
const SYSTEM_CHILDREN: NavTab[] = [
  {
    label: 'Estadísticas',
    path: APP_ROUTES.panelStatistics,
    icon: TAB_ICONS['Estadisticas'],
  },
  {
    label: 'Registro de auditoría',
    path: APP_ROUTES.panelAuditLogs,
    icon: TAB_ICONS['Auditoria'],
  },
  {
    label: 'Administración de datos',
    path: APP_ROUTES.panelDataAdministration,
    icon: TAB_ICONS['AdministracionDeDatos'],
  },
];

/**
 * Admin and Owner are a single administrative level with identical access, so
 * they share one menu. The only role with a different (empty) menu is the
 * technical Guest.
 */
const ADMIN_TABS: NavTab[] = [
  {
    label: 'Competición',
    icon: TAB_ICONS['Torneo'],
    children: COMPETITION_CHILDREN_OWNER,
  },
  {
    label: 'Gestión de equipos',
    icon: TAB_ICONS['AdministracionDeEquipos'],
    children: TEAM_CHILDREN,
  },
  { label: 'Novedades', path: APP_ROUTES.panelBlog, icon: TAB_ICONS['Blog'] },
  { label: 'Usuarios', path: APP_ROUTES.panelUsers, icon: TAB_ICONS['Usuarios'] },
  {
    label: 'Sistema',
    icon: TAB_ICONS['Administracion'],
    children: SYSTEM_CHILDREN,
  },
  {
    label: 'Configuración',
    icon: TAB_ICONS['Configuracion'],
    children: CONFIGURATION_CHILDREN,
  },
];

const TABS_BY_ROLE: Record<UserRolesType, NavTab[]> = {
  [UserRolesType.Owner]: ADMIN_TABS,
  [UserRolesType.Admin]: ADMIN_TABS,
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
    Competición: true,
    'Gestión de equipos': true,
    Sistema: false,
    Configuración: location.pathname.startsWith(APP_ROUTES.panelSettings),
  });

  // HU-105: on small screens the drawer is a temporary overlay toggled from
  // the top app bar; on md+ it stays permanent. This flag drives the overlay.
  const [mobileOpen, setMobileOpen] = React.useState(false);

  const tabs = TABS_BY_ROLE[role] ?? [];

  // Navigate and, on mobile, close the temporary drawer so it doesn't cover
  // the page the user just opened.
  const handleNavigate = (path: string) => {
    navigate(path);
    setMobileOpen(false);
  };

  // HU-03: after signing out, redirect to the public home instead of staying
  // on the (now unauthenticated) panel URL, which would render a 404.
  const handleLogOut = async () => {
    setMobileOpen(false);
    await logOut();
    navigate(APP_ROUTES.home);
  };

  const drawerContent = (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      <Toolbar sx={{ px: 2, display: 'flex', justifyContent: 'center' }}>
        <Box
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: LOGO_BACKGROUND_COLOR,
            borderRadius: 2,
            px: 1.25,
            py: 0.75,
            border: '1px solid',
            borderColor: 'rgba(255, 255, 255, 0.12)',
            boxShadow: 3,
          }}
        >
          <Box
            component="img"
            src="/assets/logo-club12.png"
            alt="Club 12"
            sx={{ height: 44, width: 'auto', display: 'block' }}
          />
        </Box>
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
                        onClick={() => child.path && handleNavigate(child.path)}
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
              onClick={() => tab.path && handleNavigate(tab.path)}
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
        <ListItemButton
          onClick={handleLogOut}
          sx={{ mx: 1, borderRadius: 1, mb: 1 }}
        >
          <ListItemIcon sx={{ minWidth: 36 }}>
            <LogoutIcon />
          </ListItemIcon>
          <ListItemText primary="Cerrar sesión" />
        </ListItemButton>
      </List>
    </Box>
  );

  const drawerPaperSx = {
    width: DRAWER_WIDTH,
    boxSizing: 'border-box',
    backgroundColor: 'background.paper',
    borderRight: '1px solid',
    borderColor: 'divider',
  } as const;

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      {/* HU-105: compact top bar shown only on small screens, hosting the
          hamburger that toggles the temporary navigation drawer. */}
      <AppBar
        position="fixed"
        color="default"
        elevation={1}
        sx={{
          display: { xs: 'flex', md: 'none' },
          backgroundColor: 'background.paper',
        }}
      >
        <Toolbar sx={{ gap: 1 }}>
          <IconButton
            edge="start"
            aria-label="Abrir menú de navegación"
            onClick={() => setMobileOpen(true)}
          >
            <MenuIcon />
          </IconButton>
          <Box
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              bgcolor: LOGO_BACKGROUND_COLOR,
              borderRadius: 1.5,
              px: 1,
              py: 0.5,
              border: '1px solid',
              borderColor: 'rgba(255, 255, 255, 0.12)',
              boxShadow: 2,
            }}
          >
            <Box
              component="img"
              src="/assets/logo-club12.png"
              alt="Club 12"
              sx={{ height: 34, width: 'auto', display: 'block' }}
            />
          </Box>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}
      >
        {/* Temporary overlay drawer for xs/sm. */}
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': drawerPaperSx,
          }}
        >
          {drawerContent}
        </Drawer>

        {/* Permanent drawer for md and up. */}
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': drawerPaperSx,
          }}
          open
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: { xs: 2, sm: 3 },
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          backgroundColor: 'background.default',
          minHeight: '100vh',
        }}
      >
        {/* Spacer that pushes content below the mobile AppBar; collapses on md+. */}
        <Toolbar sx={{ display: { xs: 'block', md: 'none' } }} />
        {children}
      </Box>
    </Box>
  );
};

export default SidebarLayout;
