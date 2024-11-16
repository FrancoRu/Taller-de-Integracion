import { createContext } from "react";
import { ProviderProps } from "../../core/types/types";
import {
  CreateTournament,
  ITournamentContextProps,
  Tournament,
} from "../type/tournament";
//import { useAuth } from "../../auth/hook/useAuth.hook";
import { AxiosError } from "axios";
import { useError } from "../../error/hooks/error.hock";
import { tournamentService } from "../service/tournament.service";

export const TournamentContext = createContext<
  ITournamentContextProps | undefined
>(undefined);

export const TournamentProvider: React.FC<ProviderProps> = ({ children }) => {
  // const { isAuthenticated } = useAuth();
  const { setError, setMessage } = useError();
  const service = tournamentService;
  async function getAllTournament(): Promise<Tournament[] | undefined> {
    try {
      const result = await service.getAll();
      return result?.data?.items as Tournament[];
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
  }

  async function createTournament(value: CreateTournament): Promise<void> {
    try {
      const result = await service.create(value);
      result && setMessage(result.status, ["Tournament create successfully"]);
    } catch (error: unknown) {
      setError(error as AxiosError);
    }
  }
  const tournamentContext: ITournamentContextProps = {
    getAllTournament,
    createTournament,
  };

  return (
    <TournamentContext.Provider value={tournamentContext}>
      {children}
    </TournamentContext.Provider>
  );
};
