import { IStatistic } from '../../types/statistics/statistic.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const statisticServiceResource = '/api/statistics'

export const statisticService = {
	postStatistic: async (statistic: IStatistic) =>
		sendPost(statisticServiceResource, statistic),
	getStatistic: async (id: string) =>
		sendGet(`${statisticServiceResource}/${id}`),
	deleteStatistic: async (id: string) =>
		sendDelete(`${statisticServiceResource}/${id}`),
	putStatistic: async (statistic: IStatistic) =>
		sendPut(`${statisticServiceResource}/${statistic.id}`, statistic)
}
