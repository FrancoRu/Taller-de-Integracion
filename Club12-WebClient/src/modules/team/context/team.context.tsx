import { createContext, ReactNode, useState } from "react";
import { ITeamContextProps } from "../type/team.d";

export const TeamContext = createContext<ITeamContextProps | undefined>(undefined);

export const TeamProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<ITeamContextProps>({});

  return (
    <TeamContext.Provider value={{ ...state, setState }}>
      {children}
    </TeamContext.Provider>
  );
};
