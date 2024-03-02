import { IDivision } from "../../types/divisions/division.d"
import { sendDelete, sendGet, sendPost, sendPut } from "../../utils/utils"

const divisionServiceResource = '/api/divisions'

export const divisionService = {
    postDivision: async (division: IDivision) => sendPost(divisionServiceResource, division) ,
    getDivision: async (id: string) => sendGet(`${divisionServiceResource}/${id}`),
    deleteDivision: async (id:string) => sendDelete(`${divisionServiceResource}/${id}`),
    putDivision: async (division: IDivision) => sendPut(`${divisionServiceResource}/${division.id}`, division)     
}