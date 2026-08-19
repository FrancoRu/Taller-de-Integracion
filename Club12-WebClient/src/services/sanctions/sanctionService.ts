import { ISanction } from '../../types/sanctions/sanction.d'
import { sendDelete, sendGet, sendPost, sendPut } from '../../utils/utils'

const sanctionServiceResource = '/api/sanctions'

export const sanctionService = {
	postSanction: async (sanction: ISanction) =>
		sendPost(sanctionServiceResource, sanction),
	getSanction: async (id: string) => sendGet(`${sanctionServiceResource}/${id}`),
	deleteSanction: async (id: string) =>
		sendDelete(`${sanctionServiceResource}/${id}`),
	putSanction: async (sanction: ISanction) =>
		sendPut(`${sanctionServiceResource}/${sanction.id}`, sanction)
}
