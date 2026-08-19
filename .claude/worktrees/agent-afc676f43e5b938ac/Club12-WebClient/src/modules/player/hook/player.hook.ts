import { useContext } from 'react';
import { PlayerContext } from '@/modules/player/context/player.context';

export const usePlayer = () => {
  const context = useContext(PlayerContext);
  if (!context) {
    throw new Error('usePlayer must be used within a PlayerProvider');
  }
  return context;
};
