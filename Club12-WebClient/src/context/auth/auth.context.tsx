import React, { createContext, useState, useEffect } from "react"
// import {
//   GetLogOutRequest,
//   loginRequest,
//   registerRequest,
//   verifyTokenRequest,
// } from "../../api/auth.request"
// import axios from 'axios'
import Cookies from "js-cookie"
import { useError } from "../../hooks/error/useError"




export const AuthContext = createContext<AuthContextProps | undefined>(
  undefined
)

export const AuthProvider: React.FC<ProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null)

  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false)

  const { setError } = useError()

  useEffect(() => {
    setIsAuthenticated(user !== null)
  }, [user])

  useEffect(() => {
    const checkToken = async () => {
      const cookies = Cookies.get()
      if (cookies.token) {
        try {
          const res = await verifyTokenRequest()
          if (!res.data) setIsAuthenticated(false)
          setIsAuthenticated(true)
          setUser({
            id: res.data._id,
            username: res.data.username,
            email: res.data.email,
          })
        } catch (error) {
          setIsAuthenticated(false)
          setUser(null)
        }
      }
    }
    checkToken()
  }, [])

  const signup = async (value: object) => {
    try {
      const res = await registerRequest(value)
      const { _id, username, email } = res.data.user
      const userData: User = { id: _id, username: username, email: email }
      setUser(userData)
    } catch (error: unknown) {
      setError(error)
    }
  }

  const sigIn = async (value: object) => {
    try {
      const res = await loginRequest(value)
      const { _id, username, email } = res.data.user
      const userData: User = { id: _id, username: username, email: email }
      setUser(userData)
    } catch (error: unknown) {
      setError(error)
    }
  }

  const logOut = async () => {
    try {
      await GetLogOutRequest()
      Cookies.remove("token")
      setIsAuthenticated(false)
      setUser(null)
    } catch (error: unknown) {
      setError(error)
    }
  }

  return (
    <AuthContext.Provider
      value={{ signup, sigIn, logOut, user, isAuthenticated }}
    >
      {children}
    </AuthContext.Provider>
  )
} 