import { useContext } from 'react';
import { SeasonContext } from '@/modules/season/context/season.context';

export const useSeason = () => {
  const context = useContext(SeasonContext);
  if (!context) {
    throw new Error('useSeason must be used within a SeasonProvider');
  }
  return context;
};
