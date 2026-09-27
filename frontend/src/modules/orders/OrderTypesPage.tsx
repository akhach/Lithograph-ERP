import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  activateOrderType,
  createOrderType,
  deactivateOrderType,
  listOrderTypes,
  updateOrderType,
  type OrderType,
} from './orderTypesApi.ts'

export function OrderTypesPage() {
  const auth = useAuth()
  const canManage = auth.hasPermission(PermissionCodes.ordersManageTypes)
  const [types, setTypes] = useState<OrderType[]>([])
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<OrderType | null>(null)
  const [creating, setCreating] = useState(false)
  const [deactivating, setDeactivating] = useState<OrderType | null>(null)

  async function load() {
    try {
      setTypes(await listOrderTypes())
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const next = await listOrderTypes()
        if (!cancelled) {
          setTypes(next)
          setError(null)
        }
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Order types
        </Typography>
        {canManage ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create order type
          </Button>
        ) : null}
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Description</TableCell>
            <TableCell>Calculator template</TableCell>
            <TableCell>Status</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {types.map((type) => (
            <TableRow key={type.id}>
              <TableCell>{type.name}</TableCell>
              <TableCell>{type.description ?? ''}</TableCell>
              <TableCell>None</TableCell>
              <TableCell>{type.isActive ? 'Active' : 'Inactive'}</TableCell>
              <TableCell>
                {canManage ? (
                  <Stack direction="row" spacing={1}>
                    <Button size="small" onClick={() => setEditing(type)}>
                      Edit
                    </Button>
                    {type.isActive ? (
                      <Button size="small" onClick={() => setDeactivating(type)}>
                        Deactivate
                      </Button>
                    ) : (
                      <Button
                        size="small"
                        onClick={() => {
                          void activateOrderType(type.id).then(() => load())
                        }}
                      >
                        Activate
                      </Button>
                    )}
                  </Stack>
                ) : null}
              </TableCell>
            </TableRow>
          ))}
          {types.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5}>No order types yet.</TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>
      {creating ? (
        <OrderTypeDialog
          title="New order type"
          onClose={() => setCreating(false)}
          onSave={async (name, description) => {
            await createOrderType({ name, description })
            setCreating(false)
            await load()
          }}
        />
      ) : null}
      {editing ? (
        <OrderTypeDialog
          title="Edit order type"
          initialName={editing.name}
          initialDescription={editing.description ?? ''}
          onClose={() => setEditing(null)}
          onSave={async (name, description) => {
            await updateOrderType(editing.id, { name, description })
            setEditing(null)
            await load()
          }}
        />
      ) : null}
      <Dialog open={deactivating !== null} onClose={() => setDeactivating(null)}>
        <DialogTitle>Deactivate order type?</DialogTitle>
        <DialogContent>
          <Typography>
            It will no longer be available for new Orders. Existing Orders will remain unchanged.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeactivating(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => {
              if (!deactivating) return
              void deactivateOrderType(deactivating.id)
                .then(() => {
                  setDeactivating(null)
                  return load()
                })
                .catch((caught: unknown) => setError(messageOf(caught)))
            }}
          >
            Deactivate
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

function OrderTypeDialog({
  title,
  initialName = '',
  initialDescription = '',
  onClose,
  onSave,
}: {
  title: string
  initialName?: string
  initialDescription?: string
  onClose: () => void
  onSave: (name: string, description: string) => Promise<void>
}) {
  const [name, setName] = useState(initialName)
  const [description, setDescription] = useState(initialDescription)
  const [error, setError] = useState<string | null>(null)
  const [nameError, setNameError] = useState<string | undefined>()
  const [saving, setSaving] = useState(false)

  async function submit() {
    setSaving(true)
    setError(null)
    setNameError(undefined)
    try {
      await onSave(name, description)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setNameError(fieldMessage(caught, 'name'))
      setSaving(false)
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          <TextField
            label="Name"
            required
            value={name}
            error={Boolean(nameError)}
            helperText={nameError}
            onChange={(event) => setName(event.target.value)}
          />
          <TextField
            label="Description"
            multiline
            minRows={2}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
          <TextField label="Calculator template" value="None" disabled />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={saving} onClick={() => void submit()}>
          Save
        </Button>
      </DialogActions>
    </Dialog>
  )
}
