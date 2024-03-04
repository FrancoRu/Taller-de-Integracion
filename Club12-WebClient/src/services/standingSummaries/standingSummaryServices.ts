import { IStandingSummary } from "../../types/standingSummaries/standingSummary.d"
import { sendDelete, sendGet, sendPost, sendPut } from "../../utils/utils"

const standingSummaryServiceResource = '/api/standingSummaries'

export const standingSummaryService = {
    poststandingSummary: async (standingSummary: IStandingSummary) => sendPost(standingSummaryServiceResource, standingSummary) ,
    getstandingSummary: async (id: string) => sendGet(`${standingSummaryServiceResource}/${id}`),
    deletestandingSummary: async (id:string) => sendDelete(`${standingSummaryServiceResource}/${id}`),
    putstandingSummary: async (standingSummary: IStandingSummary) => sendPut(`${standingSummaryServiceResource}/${standingSummary.id}`, standingSummary)     
}