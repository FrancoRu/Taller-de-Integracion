import { AxiosError } from "axios";
import { createContext, ReactNode } from "react";
import { GenericResponsePagination } from "../../core/types/types";
import { useError } from "../../error/hooks/error.hock";
import { playerService } from "../service/player.service";
import {
  AddPlayerRequest,
  IPlayerContextProps,
  PlayerFiltered,
  PlayerResponse,
  PutPlayerRequest,
} from "../type/player.d";

export const PlayerContext = createContext<IPlayerContextProps | undefined>(
  undefined
);

export const PlayerProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const { setError } = useError();

  const addPlayer = async (
    player: AddPlayerRequest
  ): Promise<PlayerResponse | void> => {
    try {
      await playerService.addPlayer(player);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const getPlayerById = async (id: string): Promise<PlayerResponse | void> => {
    try {
      await playerService.getPlayerById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const getPlayersByFilter = async (
    filter: PlayerFiltered
  ): Promise<GenericResponsePagination<PlayerResponse> | void> => {
    try {
      await playerService.getPlayersByFilter(filter);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };
  const putPlayerById = async (
    id: string,
    player: PutPlayerRequest
  ): Promise<void> => {
    try {
      await playerService.putPlayerById(id, player);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const deletePlayerById = async (id: string): Promise<void> => {
    try {
      await playerService.deletePlayerById(id);
    } catch (error: unknown) {
      if (error instanceof AxiosError) {
        setError(error);
      } else {
        setError(new AxiosError("An unknown error occurred"));
      }
    }
  };

  const container: IPlayerContextProps = {
    addPlayer,
    getPlayerById,
    getPlayersByFilter,
    putPlayerById,
    deletePlayerById,
  };
  return (
    <PlayerContext.Provider value={container}>
      {children}s
    </PlayerContext.Provider>
  );
};
