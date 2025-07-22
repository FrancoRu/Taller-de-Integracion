import { DivisionProvider } from '@/modules/division/context/division.context';
import React from 'react';
import { Outlet } from 'react-router-dom';

export const TournamentIndex: React.FC = () => {
  return (
    <DivisionProvider>
      <Outlet />
    </DivisionProvider>
  );
};
