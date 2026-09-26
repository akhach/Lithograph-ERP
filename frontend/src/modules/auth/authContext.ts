import { createContext, useContext } from 'react'
import type { CurrentUser } from './authTypes.ts'

export type AuthStatus = 'loading' | 'setup' | 'anonymous' | 'authenticated' | 'error'

export type AuthContextValue = {
  status: AuthStatus
  user: CurrentUser | null
  errorMessage: string | null
  isAuthenticated: boolean
  isLoading: boolean
  permissions: string[]
  hasPermission: (code: string) => boolean
  refresh: () => Promise<void>
  completeSetup: (password: string) => Promise<void>
  signIn: (username: string, password: string) => Promise<void>
  signOut: () => Promise<void>
  reloadUser: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | null>(null)

export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext)
  if (!value) {
    throw new Error('useAuth must be used within AuthProvider.')
  }
  return value
}
