import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { useState } from 'react'
import { changePassword } from './authApi.ts'
import { fieldMessage, messageOf, passwordsMatch } from './apiMessages.ts'

export function ChangePasswordDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [done, setDone] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  function resetAndClose() {
    setCurrentPassword('')
    setNewPassword('')
    setConfirmation('')
    setError(null)
    setDone(false)
    onClose()
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    const confirmationError = passwordsMatch(newPassword, confirmation)
    if (confirmationError) {
      setError(new Error(confirmationError))
      return
    }

    setSubmitting(true)
    setError(null)
    try {
      await changePassword(currentPassword, newPassword)
      setDone(true)
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={resetAndClose} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>Change password</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Current password"
              type="password"
              value={currentPassword}
              onChange={(event) => setCurrentPassword(event.target.value)}
              error={Boolean(fieldMessage(error, 'currentPassword'))}
              helperText={fieldMessage(error, 'currentPassword')}
              autoComplete="current-password"
              required
            />
            <TextField
              label="New password"
              type="password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
              error={Boolean(fieldMessage(error, 'newPassword'))}
              helperText={fieldMessage(error, 'newPassword')}
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
            {done ? (
              <Alert severity="success">Password changed. Other sessions were signed out.</Alert>
            ) : null}
            {error &&
            !fieldMessage(error, 'currentPassword') &&
            !fieldMessage(error, 'newPassword') ? (
              <Alert severity="error">{messageOf(error)}</Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={resetAndClose}>Close</Button>
          <Button type="submit" variant="contained" disabled={submitting || done}>
            Change password
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
