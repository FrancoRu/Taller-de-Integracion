export interface ISanctionPlayer{
    id: string, 
    duration: number,
    issuedDate: Date,
    playerId: string,
    sanctionId: string
}

export interface IBaseSanctionPlayer{ 
    jerseyNumber?: number,
    startDate?: Date,
    playerId?: string,
    sanctionId?: string
}

