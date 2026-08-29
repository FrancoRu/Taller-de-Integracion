/**
 * Single source of truth for every frontend page path. Static paths are
 * plain strings (used directly in Route definitions, nav links and
 * navigate() calls). Dynamic paths expose both a `pattern` (with the
 * :param placeholder, for Route definitions) and a `build` function (for
 * navigate() calls with a real id).
 */
export const APP_ROUTES = {
  home: '/',
  login: '/login',
  forgotPassword: '/auth/olvide-password',
  activate: '/auth/activar',
  passwordReset: '/auth/password-reset',
  quienesSomos: '/quienes-somos',
  fichaMedica: '/ficha-medica',
  reglamento: '/reglamento',
  forbidden: '/forbidden',

  publicTeam: {
    pattern: '/equipos/:teamId',
    build: (teamId: string) => `/equipos/${teamId}`,
  },
  publicSanctions: '/sanciones',
  publicChampions: '/campeones',
  publicMatch: {
    pattern: '/partidos/:matchId',
    build: (matchId: string) => `/partidos/${matchId}`,
  },
  publicSeasons: '/temporadas',
  publicSeason: {
    pattern: '/temporadas/:seasonId',
    build: (seasonId: string) => `/temporadas/${seasonId}`,
  },
  publicTournaments: '/torneos',
  publicTournament: {
    pattern: '/torneos/:tournamentId',
    build: (tournamentId: string) => `/torneos/${tournamentId}`,
  },
  publicBlog: '/blog',
  blogPost: {
    pattern: '/blog/:idOrSlug',
    build: (idOrSlug: string) => `/blog/${idOrSlug}`,
  },

  panel: '/panel',
  panelPlayers: '/panel/jugadores',
  panelPlayer: {
    pattern: '/panel/jugadores/:playerId',
    build: (playerId: string) => `/panel/jugadores/${playerId}`,
  },
  panelTeam: '/panel/equipo',
  panelTeamDetail: {
    pattern: '/panel/equipos/:teamId',
    build: (teamId: string) => `/panel/equipos/${teamId}`,
  },
  panelTournament: '/panel/torneo',
  panelTournamentDetail: {
    pattern: '/panel/torneos/:tournamentId',
    build: (tournamentId: string) => `/panel/torneos/${tournamentId}`,
  },
  panelTournamentEdit: {
    pattern: '/panel/torneos/:tournamentId/editar',
    build: (tournamentId: string) => `/panel/torneos/${tournamentId}/editar`,
  },
  panelTeams: '/panel/equipos',
  panelClub: {
    pattern: '/panel/clubes/:idOrSlug',
    build: (idOrSlug: string) => `/panel/clubes/${idOrSlug}`,
  },
  panelTeamRegister: '/panel/registro-equipos',
  panelSanctions: '/panel/sanciones',
  panelSanction: {
    pattern: '/panel/sanciones/:playerSanctionId',
    build: (playerSanctionId: string) => `/panel/sanciones/${playerSanctionId}`,
  },
  panelSanctionEdit: {
    pattern: '/panel/sanciones/editar/:playerSanctionId',
    build: (playerSanctionId: string) =>
      `/panel/sanciones/editar/${playerSanctionId}`,
  },
  panelScorers: '/panel/puntuaciones',
  panelVenues: '/panel/canchas',
  panelVenue: {
    pattern: '/panel/canchas/:venueId',
    build: (venueId: string) => `/panel/canchas/${venueId}`,
  },
  panelSeasons: '/panel/temporadas',
  panelTournaments: '/panel/torneos',
  panelTournamentWizard: '/panel/torneos/asistente',
  panelDivisions: '/panel/divisiones',
  panelDivisionCreate: '/panel/divisiones/crear',
  panelDivisionEdit: {
    pattern: '/panel/divisiones/:divisionId/editar',
    build: (divisionId: string) => `/panel/divisiones/${divisionId}/editar`,
  },
  panelDivision: {
    pattern: '/panel/divisiones/:divisionId',
    build: (divisionId: string) => `/panel/divisiones/${divisionId}`,
  },
  panelStages: '/panel/fases',
  panelStageCreate: '/panel/fases/crear',
  panelStage: {
    pattern: '/panel/fases/:stageId',
    build: (stageId: string) => `/panel/fases/${stageId}`,
  },
  panelMatches: '/panel/partidos',
  panelMatchCreate: '/panel/partidos/crear',
  panelMatch: {
    pattern: '/panel/partidos/:matchId',
    build: (matchId: string) => `/panel/partidos/${matchId}`,
  },
  panelBlog: '/panel/blog',
  panelBlogCreate: '/panel/blog/crear',
  panelBlogEdit: {
    pattern: '/panel/blog/:blogPostId/editar',
    build: (blogPostId: string) => `/panel/blog/${blogPostId}/editar`,
  },
  panelUsers: '/panel/usuarios',
  panelUserCreate: '/panel/usuarios/crear',
  panelUserInvite: '/panel/usuarios/invitar',
  panelUserEdit: {
    pattern: '/panel/usuarios/:userId/editar',
    build: (userId: string) => `/panel/usuarios/${userId}/editar`,
  },
  panelUser: {
    pattern: '/panel/usuarios/:userId',
    build: (userId: string) => `/panel/usuarios/${userId}`,
  },
  panelSettings: '/panel/configuracion',
  panelChangePassword: '/panel/configuracion/cambiar-password',
  panelEditProfile: '/panel/configuracion/editar-perfil',
  panelStatistics: '/panel/estadisticas',
  panelAuditLogs: '/panel/auditoria',
  panelDataAdministration: '/panel/test',
} as const;
