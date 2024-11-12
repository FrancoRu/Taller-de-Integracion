import { IMatch } from '../../types/matchs/match.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const matchServiceResource = '/api/matchs'

export const matchService = {
	postMatch: async (match: IMatch) => sendPost(matchServiceResource, match),
	getMatch: async (id: string) => sendGet(`${matchServiceResource}/${id}`),
	deleteMatch: async (id: string) => sendDelete(`${matchServiceResource}/${id}`),
	putMatch: async (match: IMatch) =>
		sendPut(`${matchServiceResource}/${match.id}`, match)
}
