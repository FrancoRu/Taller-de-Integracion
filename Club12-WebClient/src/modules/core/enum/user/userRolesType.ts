export const UserRolesType = {
  Admin: 'Admin',
  Owner: 'Owner',
  TournamentManager: 'TournamentManager',
  TeamManager: 'TeamManager',
  Guest: 'Guest',
} as const;

export type UserRolesType = (typeof UserRolesType)[keyof typeof UserRolesType];
