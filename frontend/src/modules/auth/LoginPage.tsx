import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { ApiError } from '../../api/apiClient.ts'
import { messageOf } from './apiMessages.ts'
import { AuthScreen } from './AuthScreen.tsx'
import { useAuth } from './authContext.ts'
import { SessionStatus } from './SessionStatus.tsx'

export function LoginPage() {
  return (
    <SessionStatus expect="anonymous">
      <LoginForm />
    </SessionStatus>
  )
}

function LoginForm() {
  const auth = useAuth()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await auth.signIn(username, password)
    } catch (caught: unknown) {
      setError(
        caught instanceof ApiError && caught.code === 'INVALID_CREDENTIALS'
          ? 'The username or password is incorrect.'
          : messageOf(caught),
      )
      setSubmitting(false)
    }
  }

  return (
    <AuthScreen title="Sign in">
      <form onSubmit={(event) => void onSubmit(event)}>
        <Stack spacing={2}>
          <TextField
            label="Username"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            autoComplete="username"
            required
          />
          <TextField
            label="Password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
            required
          />
          {error ? <Alert severity="error">{error}</Alert> : null}
          <Button type="submit" variant="contained" disabled={submitting}>
            Sign in
          </Button>
        </Stack>
      </form>
    </AuthScreen>
  )
}
