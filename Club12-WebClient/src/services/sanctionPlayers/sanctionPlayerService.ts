import { ISanctionPlayer } from "../../types/sanctionPlayers/sanctionPlayer.d"
import { sendDelete, sendGet, sendPost, sendPut } from "../../utils/utils"

const sanctionPlayerResource = '/api/sanctionPlayers'

export const sanctionPlayerService = {
    postSanctionPlayer: async (sanctionPlayer: ISanctionPlayer) => sendPost(sanctionPlayerResource, sanctionPlayer) ,
    getSanctionPlayer: async (id: string) => sendGet(`${sanctionPlayerResource}/${id}`),
    deleteSanctionPlayer: async (id:string) => sendDelete(`${sanctionPlayerResource}/${id}`),
    putSanctionPlayer: async (sanctionPlayer: ISanctionPlayer) => sendPut(`${sanctionPlayerResource}/${sanctionPlayer.id}`, sanctionPlayer)     
}