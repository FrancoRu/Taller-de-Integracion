export const UserRolesType = {
  Admin: 'ADMIN',
  Owner: 'OWNER',
  TournamentManager: 'TOURNAMENT_MANAGER',
  TeamManager: 'TEAM_MANAGER',
  Guest: 'GUEST',
} as const;

export type UserRolesType = (typeof UserRolesType)[keyof typeof UserRolesType];

/**
 * Canonical Spanish display labels for every role. Co-located with the enum
 * so every view (users list, create-user form, edit-user form) shows the
 * same wording instead of each screen inventing its own.
 */
export const USER_ROLE_LABELS: Record<UserRolesType, string> = {
  [UserRolesType.Admin]: 'Admin',
  [UserRolesType.Owner]: 'Owner',
  [UserRolesType.TournamentManager]: 'Responsable del Torneo',
  [UserRolesType.TeamManager]: 'Responsable de Equipo',
  [UserRolesType.Guest]: 'Invitado',
};
