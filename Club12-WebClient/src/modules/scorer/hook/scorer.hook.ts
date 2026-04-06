import { useContext } from 'react';
import { ScorerContext } from '@/modules/scorer/context/scorer.context';

export const useScorer = () => {
  const context = useContext(ScorerContext);
  if (!context) {
    throw new Error('useScorer must be used within a ScorerProvider');
  }
  return context;
};
