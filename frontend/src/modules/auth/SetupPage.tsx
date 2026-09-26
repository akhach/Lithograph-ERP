import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { messageOf, passwordsMatch } from './apiMessages.ts'
import { AuthScreen } from './AuthScreen.tsx'
import { useAuth } from './authContext.ts'
import { SessionStatus } from './SessionStatus.tsx'

export function SetupPage() {
  return (
    <SessionStatus expect="setup">
      <SetupForm />
    </SessionStatus>
  )
}

function SetupForm() {
  const auth = useAuth()
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    const confirmationError = passwordsMatch(password, confirmation)
    if (confirmationError) {
      setError(confirmationError)
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      await auth.completeSetup(password)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setSubmitting(false)
    }
  }

  return (
    <AuthScreen title="Initial setup">
      <form onSubmit={(event) => void onSubmit(event)}>
        <Stack spacing={2}>
          <TextField label="Username" value="director" slotProps={{ input: { readOnly: true } }} />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="new-password"
            required
          />
          <TextField
            label="Confirm password"
            type="password"
            value={confirmation}
            onChange={(event) => setConfirmation(event.target.value)}
            autoComplete="new-password"
            required
          />
          {error ? <Alert severity="error">{error}</Alert> : null}
          <Button type="submit" variant="contained" disabled={submitting}>
            Create director
          </Button>
        </Stack>
      </form>
    </AuthScreen>
  )
}
