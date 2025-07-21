import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ListItemButton, ListItemText, Divider } from '@mui/material';
import { RoutesNavigationViews } from '@/views/core/routes-const';
import { useTournament } from '@/modules/tournament/hook/tournament.hook';

interface TournamentMenuItemsProps {
  onNavigate: (path: string) => void;
}

const TournamentMenuItems: React.FC<TournamentMenuItemsProps> = ({
  onNavigate,
}) => {
  const location = useLocation();
  const { tournaments } = useTournament();
  return (
    <>
      <Divider sx={{ my: 1 }} />
      {tournaments &&
        tournaments.map(t => (
          <ListItemButton
            key={t.id}
            component={Link}
            to={`${RoutesNavigationViews.Tournament}/${t.id}`}
            selected={location.pathname.startsWith(
              `${RoutesNavigationViews.Tournament}/${t.id}`
            )}
            onClick={() =>
              onNavigate(`${RoutesNavigationViews.Tournament}/${t.id}`)
            }
          >
            <ListItemText primary={t.name} />
          </ListItemButton>
        ))}
    </>
  );
};

export default TournamentMenuItems;
