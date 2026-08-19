import { IPlayerTeam } from '../../types/playerTeams/playerTeam.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const playerTeamServiceResource = '/api/playerTeams'

export const playerTeamService = {
	postPlayerTeam: async (playerTeam: IPlayerTeam) =>
		sendPost(playerTeamServiceResource, playerTeam),
	getPlayerTeam: async (id: string) =>
		sendGet(`${playerTeamServiceResource}/${id}`),
	deletePlayerTeam: async (id: string) =>
		sendDelete(`${playerTeamServiceResource}/${id}`),
	putPlayerTeam: async (playerTeam: IPlayerTeam) =>
		sendPut(`${playerTeamServiceResource}/${playerTeam.id}`, playerTeam)
}
