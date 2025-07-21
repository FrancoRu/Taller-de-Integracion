import { createContext, ReactNode, useState } from "react";
import { IStageContextProps } from "../type/stage.d";

export const StageContext = createContext<IStageContextProps | undefined>(undefined);

export const StageProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<IStageContextProps>({});

  return (
    <StageContext.Provider value={{ ...state, setState }}>
      {children}
    </StageContext.Provider>
  );
};
