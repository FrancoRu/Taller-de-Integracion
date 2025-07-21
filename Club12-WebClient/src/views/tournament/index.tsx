import { DivisionProvider } from '@/modules/division/context/division.context';
import { TournamentProvider } from '@/modules/tournament/context/tournament.context';
import React from 'react';
import { Outlet } from 'react-router-dom';

export const TournamentIndex: React.FC = () => {
  return (
    <TournamentProvider>
      <DivisionProvider>
        <Outlet />
      </DivisionProvider>
    </TournamentProvider>
  );
};
