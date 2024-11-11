import envVariables from "../../core/constants/envVariables";
import { sendGet, sendPost } from "../../core/utils/utilsAxios";
import { CreateTournament } from "../type/tournament";

export const tournamentService = {
  getAll: async () => sendGet(envVariables.tournamentUrl),
  create: async (value: CreateTournament) =>
    sendPost(envVariables.tournamentUrl, value),
};
