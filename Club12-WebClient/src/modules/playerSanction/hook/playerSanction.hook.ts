import { useContext } from 'react';
import { PlayerSanctionContext } from '@/modules/playerSanction/context/playerSanction.context';

export const usePlayerSanction = () => {
  const context = useContext(PlayerSanctionContext);
  if (!context) {
    throw new Error(
      'usePlayerSanction must be used within a PlayerSanctionProvider'
    );
  }
  return context;
};
