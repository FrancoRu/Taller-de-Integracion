import { Typography } from '@mui/material';
import { CustomBox } from '../core/MUI/customsThemes/CustomBox';
import { NoPlayerSanctionMessageProps } from '@/modules/playerSanction/type/playerSanction';
import { useCallback } from 'react';

/**
 * Component that shows a message when no player sanctions are found,
 * optionally including the player's name if provided.
 */
export const NoPlayerSanctionMessage: React.FC<
  NoPlayerSanctionMessageProps
> = ({ name }) => {
  /**
   * Formats the player part of the message if the name is not empty.
   * @param name The player's name.
   * @returns The formatted string or empty string.
   */
  const formatPlayerPart = useCallback(
    (name?: string): string => (name?.trim() ? ` para el jugador ${name}` : ''),
    [name]
  );

  return (
    <CustomBox>
      <Typography>
        No se encontraron sanciones cargadas{formatPlayerPart(name)} todavía.
      </Typography>
    </CustomBox>
  );
};
