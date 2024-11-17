import { createContext, ReactNode, useState } from "react";
import { IVenueContextProps } from "../type/Venue.d";

export const VenueContext = createContext<IVenueContextProps | undefined>(undefined);

export const VenueProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [state, setState] = useState<IVenueContextProps>({});

  return (
    <VenueContext.Provider value={{ ...state, setState }}>
      {children}
    </VenueContext.Provider>
  );
};
