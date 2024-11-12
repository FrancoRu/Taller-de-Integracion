import { IPlayerStatistic } from '../../types/playerStatistics/playerStatistic.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const playerStatisticServiceResource = '/api/playerStatistics'

export const playerStatisticService = {
	postPlayerStatistic: async (playerStatistic: IPlayerStatistic) =>
		sendPost(playerStatisticServiceResource, playerStatistic),
	getPlayerStatistic: async (id: string) =>
		sendGet(`${playerStatisticServiceResource}/${id}`),
	deletePlayerStatistic: async (id: string) =>
		sendDelete(`${playerStatisticServiceResource}/${id}`),
	putPlayerStatistic: async (playerStatistic: IPlayerStatistic) =>
		sendPut(
			`${playerStatisticServiceResource}/${playerStatistic.id}`,
			playerStatistic
		)
}
