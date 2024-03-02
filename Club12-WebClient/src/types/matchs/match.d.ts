export interface IMatch{
    id: string,
    datetime: Date,
    homeScore: number,
    visitorScore: number,
    homeTeamId: string,
    visitorTeamId: string,
    winningTeamId: string
}

export interface IBaseMatch{
    datetime?: Date,
    homeScore?: number,
    visitorScore?: number,
    homeTeamId?: string,
    visitorTeamId?: string,
    winningTeamId?: string
}

