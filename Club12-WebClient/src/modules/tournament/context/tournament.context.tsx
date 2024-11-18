import { createContext } from "react";
import { ProviderProps } from "../../core/types/types";
import { ITournamentContextProps } from "../type/tournament";
//import { useAuth } from "../../auth/hook/useAuth.hook";
import { useError } from "../../error/hooks/error.hock";

export const TournamentContext = createContext<
  ITournamentContextProps | undefined
>(undefined);

export const TournamentProvider: React.FC<ProviderProps> = ({ children }) => {
  // const { isAuthenticated } = useAuth();
  const { setError, setMessage } = useError();

  return (
    <TournamentContext.Provider value={}>{children}</TournamentContext.Provider>
  );
};
