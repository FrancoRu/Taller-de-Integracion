export const TournamentStatus = {
  Scheduled: 'Scheduled',
  OpenForRegistration: 'OpenForRegistration',
  Ongoing: 'Ongoing',
  Finished: 'Finished',
  Canceled: 'Canceled',
} as const;

export type TournamentStatus =
  (typeof TournamentStatus)[keyof typeof TournamentStatus];
