import Alert from '@mui/material/Alert'
import CircularProgress from '@mui/material/CircularProgress'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { getHealth, type HealthResponse } from '../api/health.ts'

type HealthState =
  | { status: 'loading' }
  | { status: 'success'; health: HealthResponse }
  | { status: 'error'; message: string }

export function DevelopmentStatusPage() {
  const [state, setState] = useState<HealthState>({ status: 'loading' })

  useEffect(() => {
    const controller = new AbortController()

    getHealth(controller.signal)
      .then((health) => setState({ status: 'success', health }))
      .catch((error: unknown) => {
        if (controller.signal.aborted) {
          return
        }
        const message = error instanceof Error ? error.message : 'Backend is not reachable.'
        setState({ status: 'error', message })
      })

    return () => controller.abort()
  }, [])

  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Stack spacing={2}>
        <Typography variant="h5" component="h2">
          Lithograph ERP
        </Typography>
        <Typography>Frontend is running.</Typography>

        {state.status === 'loading' && (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <CircularProgress size={18} />
            <Typography>Checking backend health...</Typography>
          </Stack>
        )}

        {state.status === 'success' && (
          <Alert severity={state.health.database === 'Healthy' ? 'success' : 'warning'}>
            Backend: {state.health.application}. Database: {state.health.database}.
          </Alert>
        )}

        {state.status === 'error' && (
          <Alert severity="error">Backend health check failed: {state.message}</Alert>
        )}
      </Stack>
    </Paper>
  )
}
