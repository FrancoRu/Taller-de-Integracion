import { useContext } from 'react';
import { TournamentContext } from '@/modules/tournament/context/tournament.context';

export const useTournament = () => {
  const context = useContext(TournamentContext);
  if (!context) {
    throw new Error(
      'useTournament must be used whithin an Tournament Provider'
    );
  }
  return context;
};
