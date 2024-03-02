import { BaseTeam, PostTeam } from '../types/team'
import { sendDelete, sendGet, sendPost, sendPut } from './utils'

const teamPostResource = 'team'

export const teamPostService = {
	post: async (team: PostTeam) =>
		sendPost(`${teamPostResource}/`, {
			Name: team.Name,
			ThreeLetterCode: team.ThreeLetterCode,
			Division: team.Division,
		}),
}

export const teamPutService = {
	put: async (team: BaseTeam) => sendGet(`${teamPostResource}/`),
}
