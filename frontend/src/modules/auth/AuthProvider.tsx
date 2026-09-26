import { useCallback, useEffect, useMemo, useState } from 'react'
import { Outlet } from 'react-router'
import { ApiError, setUnauthorizedHandler } from '../../api/apiClient.ts'
import { AuthContext, type AuthStatus } from './authContext.ts'
import { getCurrentUser, getSetupStatus, login, logout, setupDirector } from './authApi.ts'
import type { CurrentUser } from './authTypes.ts'

type SessionState = {
  status: AuthStatus
  user: CurrentUser | null
  errorMessage: string | null
}

const loadingState: SessionState = { status: 'loading', user: null, errorMessage: null }
const noPermissions: string[] = []

async function readSession(): Promise<SessionState> {
  try {
    const status = await getSetupStatus()
    if (status.requiresSetup) {
      return { status: 'setup', user: null, errorMessage: null }
    }
    const user = await getCurrentUser()
    return { status: 'authenticated', user, errorMessage: null }
  } catch (error: unknown) {
    if (error instanceof ApiError && error.status === 401) {
      return { status: 'anonymous', user: null, errorMessage: null }
    }
    return { status: 'error', user: null, errorMessage: failureMessage(error) }
  }
}

function failureMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }
  if (error instanceof TypeError) {
    return 'Could not reach the server.'
  }
  return 'Something went wrong.'
}

export function AuthProvider() {
  const [session, setSession] = useState<SessionState>(loadingState)

  const refresh = useCallback(async () => {
    setSession(loadingState)
    setSession(await readSession())
  }, [])

  const reloadUser = useCallback(async () => {
    const user = await getCurrentUser()
    setSession({ status: 'authenticated', user, errorMessage: null })
  }, [])

  const completeSetup = useCallback(async (password: string) => {
    await setupDirector(password)
    setSession({ status: 'anonymous', user: null, errorMessage: null })
  }, [])

  const signIn = useCallback(async (username: string, password: string) => {
    const user = await login(username, password)
    setSession({ status: 'authenticated', user, errorMessage: null })
  }, [])

  const signOut = useCallback(async () => {
    try {
      await logout()
    } finally {
      setSession({ status: 'anonymous', user: null, errorMessage: null })
    }
  }, [])

  useEffect(() => {
    setUnauthorizedHandler(() => {
      setSession({ status: 'anonymous', user: null, errorMessage: null })
    })
    return () => setUnauthorizedHandler(null)
  }, [])

  useEffect(() => {
    let cancelled = false
    void readSession().then((next) => {
      if (!cancelled) {
        setSession(next)
      }
    })
    return () => {
      cancelled = true
    }
  }, [])

  const value = useMemo(() => {
    const permissions = session.user?.permissions ?? noPermissions
    return {
      status: session.status,
      user: session.user,
      errorMessage: session.errorMessage,
      isAuthenticated: session.status === 'authenticated',
      isLoading: session.status === 'loading',
      permissions,
      hasPermission: (code: string) => permissions.includes(code),
      refresh,
      completeSetup,
      signIn,
      signOut,
      reloadUser,
    }
  }, [session, refresh, completeSetup, signIn, signOut, reloadUser])

  return (
    <AuthContext.Provider value={value}>
      <Outlet />
    </AuthContext.Provider>
  )
}
