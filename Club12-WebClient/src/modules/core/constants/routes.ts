const routes = {
  apiUrl: `https://localhost:${import.meta.env.VITE_BACKEND_PORT}/api`,

  blogposts: 'blogposts',
  divisions: 'divisions',
  stages: 'stages',
  matches: 'matches',
  players: 'players',
  playerSanctions: 'player-sanctions',
  playerStatistics: 'player-statistics',
  staff: 'staffs',
  teams: 'teams',
  tournaments: 'tournaments',
  users: 'users',
  auth: 'auth',
  venues: 'venues',
};

export default routes;
