import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import { Navigate } from 'react-router'
import { useAuth } from './authContext.ts'

export function SessionStatus({
  children,
  expect,
}: {
  children?: React.ReactNode
  expect: 'setup' | 'anonymous' | 'authenticated'
}) {
  const auth = useAuth()

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
      return <Navigate to="/" replace />
    }
    return <Navigate to="/login" replace />
  }

  return children
}
