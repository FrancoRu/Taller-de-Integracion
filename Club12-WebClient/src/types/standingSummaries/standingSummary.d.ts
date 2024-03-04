export interface IStandingSummary{
    id: string, 
    position: number,
    tournamentId: string,
    teamId: string
}

export interface IBaseStandingSummary{ 
    position?: number,
    tournamentId?: string,
    teamId?: string
}