import { createContext, ReactNode, useState } from "react";
import { IMatchContextProps } from "../type/match";

export const MatchContext = createContext<IMatchContextProps | undefined>(
  undefined
);

export const MatchProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [state, setState] = useState<IMatchContextProps>({});

  return (
    <MatchContext.Provider value={{ ...state, setState }}>
      {children}
    </MatchContext.Provider>
  );
};
