import { createContext, ReactNode } from "react";
import { IPlayerContextProps } from "../type/player.d";

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const getPlayerById = (id: number): void => {
    console.log(id);
  };
  return (
    <PlayerContext.Provider value={{ getPlayerById }}>
      {children}s
    </PlayerContext.Provider>
  );
};
