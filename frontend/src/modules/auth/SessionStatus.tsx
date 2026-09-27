import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import { Navigate, useLocation } from 'react-router'
import { useAuth } from './authContext.ts'

export function SessionStatus({
  children,
  expect,
}: {
  children?: React.ReactNode
  expect: 'setup' | 'anonymous' | 'authenticated'
}) {
  const auth = useAuth()
  const location = useLocation()

  if (auth.status === 'loading') {
    return (
      <Box
        sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}
      >
        <CircularProgress />
      </Box>
    )
  }

  if (auth.status === 'error') {
    return (
      <Box
        sx={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: 2,
        }}
      >
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={() => void auth.refresh()}>
              Retry
            </Button>
          }
        >
          {auth.errorMessage}
        </Alert>
      </Box>
    )
  }

  if (auth.status !== expect) {
    if (auth.status === 'setup') {
      return <Navigate to="/setup" replace />
    }
    if (auth.status === 'authenticated') {
      const requested = new URLSearchParams(location.search).get('return')
      return <Navigate to={safeReturnPath(requested) ?? '/dashboard'} replace />
    }
    if (expect === 'authenticated') {
      const current = `${location.pathname}${location.search}`
      return <Navigate to={`/login?return=${encodeURIComponent(current)}`} replace />
    }
    return <Navigate to="/login" replace />
  }

  return children
}

function safeReturnPath(value: string | null): string | null {
  if (
    !value ||
    !value.startsWith('/') ||
    value.startsWith('//') ||
    value.includes('\\') ||
    value.includes('://')
  ) {
    return null
  }
  const path = value.split('?')[0] ?? ''
  if (path === '/' || path === '/dashboard') {
    return '/dashboard'
  }
  if (
    path === '/login' ||
    path.startsWith('/login/') ||
    path === '/setup' ||
    path.startsWith('/setup/')
  ) {
    return null
  }
  if (path.split('/').includes('..')) {
    return null
  }
  return value
}
