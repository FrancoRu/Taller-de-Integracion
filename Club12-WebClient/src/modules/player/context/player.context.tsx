import { createContext, ReactNode, useState } from "react";
import { IPlayerContextProps } from "../type/player.d";

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [state, setState] = useState<IPlayerContextProps>({});

  return (
    <PlayerContext.Provider value={{ ...state, setState }}>
      {children}
    </PlayerContext.Provider>
  );
};
