export interface IPlayerTeam{
    id: string, 
    jerseyNumber: number,
    startDate: Date,
    playerId: string,
    teamId: string
}

export interface IBasePlayerTeam{ 
    jerseyNumber?: number,
    startDate?: Date,
    playerId?: string,
    teamId?: string
}
