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
  const hasLogo = Boolean(logoUrl);

  return (
    <Avatar
      src={logoUrl ?? undefined}
      alt={`Logo de ${teamName}`}
      sx={{
        width: size,
        height: size,
        fontSize: size * 0.42,
        // Keep a solid backdrop only for the initial-letter fallback. A real
        // crest is usually a transparent PNG, so it must sit on a transparent
        // background — otherwise the brand colour shows through behind it.
        bgcolor: hasLogo ? 'transparent' : 'primary.dark',
        '& .MuiAvatar-img': { objectFit: 'contain' },
      }}
    >
      {initial}
    </Avatar>
  );
};

export default TeamLogo;
