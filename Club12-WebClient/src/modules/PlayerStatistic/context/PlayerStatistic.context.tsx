import { createContext, ReactNode, useState } from "react";
import { IPlayerStatisticContextProps } from "../type/playerStatistic";

export const PlayerStatisticContext = createContext<
  IPlayerStatisticContextProps | undefined
>(undefined);

export const PlayerStatisticProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [state, setState] = useState<IPlayerStatisticContextProps>({});

  return (
    <PlayerStatisticContext.Provider value={{ ...state, setState }}>
      {children}
    </PlayerStatisticContext.Provider>
  );
};
