export interface IUser {
  id: string
  username: string
  email: string
}
  
export interface IBaseDivisionAuthContextProps {
  signup: (value: object) => Promise<void>
  sigIn: (value: object) => Promise<void>
  logOut: () => Promise<void>
  user: User | null
  isAuthenticated: boolean
}