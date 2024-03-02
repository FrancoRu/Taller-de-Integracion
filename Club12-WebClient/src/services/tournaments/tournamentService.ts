import { ITournament } from "../../types/tournamets/tournament"
import { sendDelete, sendGet, sendPost, sendPut } from "../../utils/utils"

const TournamentServiceResource = '/api/tournaments'

export const tournamentService = {
    postTournament: async (tournament: ITournament) => sendPost(TournamentServiceResource, tournament) ,
    getTournament: async (id: string) => sendGet(`${TournamentServiceResource}/${id}`),
    deleteTournament: async (id:string) => sendDelete(`${TournamentServiceResource}/${id}`),
    putTournament: async (tournament: ITournament) => sendPut(`${TournamentServiceResource}/${tournament.id}`, tournament)     
}