import { createContext, ReactNode, useState } from "react";
import { IDivisionContextProps } from "../type/Division.d";

export const DivisionContext = createContext<IDivisionContextProps | undefined>(undefined);

export const DivisionProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<IDivisionContextProps>({});

  return (
    <DivisionContext.Provider value={{ ...state, setState }}>
      {children}
    </DivisionContext.Provider>
  );
};
