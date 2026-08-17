import React from 'react';
import { Avatar } from '@mui/material';

interface TeamLogoProps {
  teamName: string;
  logoUrl?: string | null;
  size?: number;
}

const TeamLogo: React.FC<TeamLogoProps> = ({
  teamName,
  logoUrl,
  size = 32,
}) => {
  const initial = teamName?.trim().charAt(0).toUpperCase() || '?';

  return (
    <Avatar
      src={logoUrl ?? undefined}
      alt={`Logo de ${teamName}`}
      sx={{
        width: size,
        height: size,
        fontSize: size * 0.42,
        bgcolor: 'primary.dark',
      }}
    >
      {initial}
    </Avatar>
  );
};

export default TeamLogo;
