import { IPlayer } from '../../types/players/player.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const playerServiceResource = '/api/players'

export const playerService = {
	postPlayer: async (player: IPlayer) => sendPost(playerServiceResource, player),
	getPlayer: async (id: string) => sendGet(`${playerServiceResource}/${id}`),
	deletePlayer: async (id: string) =>
		sendDelete(`${playerServiceResource}/${id}`),
	putPlayer: async (player: IPlayer) =>
		sendPut(`${playerServiceResource}/${player.id}`, player)
}
