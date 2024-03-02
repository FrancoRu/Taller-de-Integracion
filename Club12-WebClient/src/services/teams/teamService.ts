import { ITeam } from "../../types/teams/team.d"
import { sendDelete, sendGet, sendPost, sendPut } from "../../utils/utils"

const teamServiceResource = '/api/teams'

export const teamService = {
    postTeam: async (team: ITeam) => sendPost(teamServiceResource, team) ,
    getTeam: async (id: string) => sendGet(`${teamServiceResource}/${id}`),
    deleteTeam: async (id:string) => sendDelete(`${teamServiceResource}/${id}`),
    putTeam: async (team: ITeam) => sendPut(`${teamServiceResource}/${team.id}`, team)     
}