export const UserRolesType = {
  Admin: 'ADMIN',
  Owner: 'OWNER',
  TournamentManager: 'TOURNAMENT_MANAGER',
  TeamManager: 'TEAM_MANAGER',
  Guest: 'GUEST',
} as const;

export type UserRolesType = (typeof UserRolesType)[keyof typeof UserRolesType];
