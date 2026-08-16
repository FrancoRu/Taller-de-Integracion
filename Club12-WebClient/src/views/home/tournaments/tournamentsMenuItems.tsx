import React, { useEffect, useRef } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ListItemButton, ListItemText, Divider } from '@mui/material';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';
import { APP_ROUTES } from '@/modules/core/constants/appRoutes';
import { PUBLIC_LISTING_PAGE_SIZE } from '@/modules/core/constants/pagination';

interface TournamentMenuItemsProps {
  onNavigate: (path: string) => void;
}

const TournamentMenuItems: React.FC<TournamentMenuItemsProps> = ({
  onNavigate,
}) => {
  const location = useLocation();
  const { tournaments, getAllTournamentsByFilter } = useTournament();
  const getAllTournamentsRef = useRef(getAllTournamentsByFilter);

  useEffect(() => {
    getAllTournamentsRef.current = getAllTournamentsByFilter;
  }, [getAllTournamentsByFilter]);

  useEffect(() => {
    void getAllTournamentsRef.current({ pageSize: PUBLIC_LISTING_PAGE_SIZE, pageNumber: 1 });
  }, []);

  return (
    <>
      <Divider sx={{ my: 1 }} />
      {tournaments &&
        tournaments.map(t => {
          const path = APP_ROUTES.publicTournament.build(t.id);
          return (
            <ListItemButton
              key={t.id}
              component={Link}
              to={path}
              selected={location.pathname.startsWith(path)}
              onClick={() => onNavigate(path)}
            >
              <ListItemText primary={t.name} />
            </ListItemButton>
          );
        })}
    </>
  );
};

export default TournamentMenuItems;
