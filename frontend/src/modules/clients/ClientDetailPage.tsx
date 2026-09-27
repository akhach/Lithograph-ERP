import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  activateClient,
  deactivateClient,
  getClient,
  updateClient,
  type Client,
} from './clientsApi.ts'

export function ClientDetailPage() {
  const auth = useAuth()
  const { clientId = '' } = useParams()
  const canEdit = auth.hasPermission(PermissionCodes.clientsEdit)
  const canActivate = auth.hasPermission(PermissionCodes.clientsActivate)
  const [client, setClient] = useState<Client | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)
  const [confirming, setConfirming] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const loaded = await getClient(clientId)
        if (!cancelled) {
          setClient(loaded)
          setError(null)
        }
      } catch (caught: unknown) {
        if (!cancelled) {
          setClient(null)
          setError(messageOf(caught))
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [clientId])

  async function save() {
    if (!client) {
      return
    }
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const updated = await updateClient(client.id, {
        name: client.name,
        contactPerson: client.contactPerson ?? '',
        phone: client.phone ?? '',
        email: client.email ?? '',
        address: client.address ?? '',
        notes: client.notes ?? '',
      })
      setClient(updated)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        contactPerson: fieldMessage(caught, 'contactPerson'),
        phone: fieldMessage(caught, 'phone'),
        email: fieldMessage(caught, 'email'),
      })
    } finally {
      setSaving(false)
    }
  }

  async function setActive(active: boolean) {
    if (!client) {
      return
    }
    try {
      const updated = active ? await activateClient(client.id) : await deactivateClient(client.id)
      setClient(updated)
      setConfirming(false)
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  async function copyBusinessId() {
    if (client) {
      await navigator.clipboard.writeText(client.businessId)
    }
  }

  if (!client) {
    return (
      <Stack spacing={2}>
        <Button component={Link} to="/clients">
          Back to clients
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : <Typography>Loading client…</Typography>}
      </Stack>
    )
  }

  return (
    <Stack spacing={2} sx={{ maxWidth: 720 }}>
      <Button component={Link} to="/clients" sx={{ alignSelf: 'flex-start' }}>
        Back to clients
      </Button>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          {client.businessId}
        </Typography>
        <Button size="small" onClick={() => void copyBusinessId()}>
          Copy
        </Button>
      </Stack>
      <Typography variant="h6">{client.name}</Typography>
      <Typography color={client.isActive ? 'success.main' : 'text.secondary'}>
        {client.isActive ? 'Active' : 'Inactive'}
      </Typography>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <TextField
        label="Name"
        required
        disabled={!canEdit}
        value={client.name}
        error={Boolean(fieldErrors.name)}
        helperText={fieldErrors.name}
        onChange={(event) => setClient({ ...client, name: event.target.value })}
      />
      <TextField
        label="Contact person"
        disabled={!canEdit}
        value={client.contactPerson ?? ''}
        error={Boolean(fieldErrors.contactPerson)}
        helperText={fieldErrors.contactPerson}
        onChange={(event) => setClient({ ...client, contactPerson: event.target.value })}
      />
      <TextField
        label="Phone"
        disabled={!canEdit}
        value={client.phone ?? ''}
        error={Boolean(fieldErrors.phone)}
        helperText={fieldErrors.phone}
        onChange={(event) => setClient({ ...client, phone: event.target.value })}
      />
      <TextField
        label="Email"
        disabled={!canEdit}
        value={client.email ?? ''}
        error={Boolean(fieldErrors.email)}
        helperText={fieldErrors.email}
        onChange={(event) => setClient({ ...client, email: event.target.value })}
      />
      <TextField
        label="Address"
        disabled={!canEdit}
        value={client.address ?? ''}
        onChange={(event) => setClient({ ...client, address: event.target.value })}
      />
      <TextField
        label="Notes"
        disabled={!canEdit}
        multiline
        minRows={3}
        value={client.notes ?? ''}
        onChange={(event) => setClient({ ...client, notes: event.target.value })}
      />
      <Stack direction="row" spacing={1}>
        {canEdit ? (
          <Button variant="contained" disabled={saving} onClick={() => void save()}>
            Save
          </Button>
        ) : null}
        {canActivate && client.isActive ? (
          <Button color="warning" onClick={() => setConfirming(true)}>
            Deactivate
          </Button>
        ) : null}
        {canActivate && !client.isActive ? (
          <Button onClick={() => void setActive(true)}>Activate</Button>
        ) : null}
      </Stack>
      <Typography color="text.secondary">
        Projects will appear here after the Projects module is implemented.
      </Typography>
      {confirming ? (
        <Dialog open onClose={() => setConfirming(false)}>
          <DialogTitle>Deactivate client?</DialogTitle>
          <DialogContent>
            <Typography>
              The client will no longer be available for new projects. Existing historical data will
              remain unchanged.
            </Typography>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setConfirming(false)}>Cancel</Button>
            <Button color="warning" onClick={() => void setActive(false)}>
              Deactivate
            </Button>
          </DialogActions>
        </Dialog>
      ) : null}
    </Stack>
  )
}
