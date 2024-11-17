export interface IPlayerContextProps {
  getPlayerById(id: number): void;
  // Define your context properties here
}

export interface Player {
  name: string;
}
