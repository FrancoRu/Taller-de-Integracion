import { createContext, ReactNode, useState } from 'react';
import { IPlayerSanctionContextProps } from '../type/playerSanction.d';

export const PlayerSanctionContext = createContext<
  IPlayerSanctionContextProps | undefined
>(undefined);

export const PlayerSanctionProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [state, setState] = useState<IPlayerSanctionContextProps>({});

  return (
    <PlayerSanctionContext.Provider value={{ ...state, setState }}>
      {children}
    </PlayerSanctionContext.Provider>
  );
};
