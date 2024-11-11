// Define las propiedades necesarias en tu interfaz
export interface ITournamentContextProps {
  getAllTournament: () => Promise<Tournament[] | undefined>;
  createTournament: (value: CreateTournament) => Promise<any>;
}

export interface CreateTournament {
  description: string;
  name: string;
}
export interface Tournament extends CreateTournament {
  division: null;
  id: string;
}
