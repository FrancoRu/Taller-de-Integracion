export const UserRolesType = {
  Admin: 'ADMIN',
  Owner: 'OWNER',
  Guest: 'GUEST',
} as const;

export type UserRolesType = (typeof UserRolesType)[keyof typeof UserRolesType];

/**
 * Canonical Spanish display labels for every role. Co-located with the enum
 * so every view (users list, create-user form, edit-user form) shows the
 * same wording instead of each screen inventing its own.
 *
 * HU-05: the role model is reduced to Owner and Admin (plus the technical
 * Guest). `Tournament Manager` and `Team Manager` were removed from the
 * system and the UI.
 */
export const USER_ROLE_LABELS: Record<UserRolesType, string> = {
  [UserRolesType.Admin]: 'Admin',
  [UserRolesType.Owner]: 'Owner',
  [UserRolesType.Guest]: 'Invitado',
};
