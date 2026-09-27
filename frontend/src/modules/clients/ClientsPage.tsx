import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { createClient, listClients, type ClientListItem } from './clientsApi.ts'

type Filters = {
  search: string
  status: 'active' | 'inactive' | 'all'
  page: number
}

const emptyForm = {
  name: '',
  contactPerson: '',
  phone: '',
  email: '',
  address: '',
  notes: '',
}

export function ClientsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const canCreate = auth.hasPermission(PermissionCodes.clientsCreate)
  const [filters, setFilters] = useState<Filters>({ search: '', status: 'active', page: 1 })
  const [draftSearch, setDraftSearch] = useState('')
  const [clients, setClients] = useState<ClientListItem[]>([])
  const [totalPages, setTotalPages] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const page = await listClients({
          search: filters.search || undefined,
          isActive: filters.status === 'all' ? undefined : filters.status === 'active',
          page: filters.page,
          sort: 'name',
        })
        if (cancelled) {
          return
        }
        setClients(page.items)
        setTotalPages(page.totalPages)
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) {
          setError(messageOf(caught))
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [filters, reloadKey])

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Clients
        </Typography>
        {canCreate ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create client
          </Button>
        ) : null}
      </Stack>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <TextField
          label="Search clients"
          size="small"
          value={draftSearch}
          onChange={(event) => setDraftSearch(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              setFilters((current) => ({ ...current, search: draftSearch.trim(), page: 1 }))
            }
          }}
        />
        <Button
          variant="outlined"
          onClick={() =>
            setFilters((current) => ({ ...current, search: draftSearch.trim(), page: 1 }))
          }
        >
          Search
        </Button>
        <TextField
          select
          label="Status"
          size="small"
          value={filters.status}
          onChange={(event) =>
            setFilters((current) => ({
              ...current,
              status: event.target.value as Filters['status'],
              page: 1,
            }))
          }
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="active">Active</MenuItem>
          <MenuItem value="inactive">Inactive</MenuItem>
          <MenuItem value="all">All</MenuItem>
        </TextField>
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Client ID</TableCell>
            <TableCell>Name</TableCell>
            <TableCell>Contact person</TableCell>
            <TableCell>Phone</TableCell>
            <TableCell>Email</TableCell>
            <TableCell>Status</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {clients.map((client) => (
            <TableRow
              key={client.id}
              hover
              onDoubleClick={() => navigate(`/clients/${client.id}`)}
              sx={{ cursor: 'pointer' }}
            >
              <TableCell>{client.businessId}</TableCell>
              <TableCell>{client.name}</TableCell>
              <TableCell>{client.contactPerson}</TableCell>
              <TableCell>{client.phone}</TableCell>
              <TableCell>{client.email}</TableCell>
              <TableCell>{client.isActive ? 'Active' : 'Inactive'}</TableCell>
              <TableCell>
                <Button size="small" onClick={() => navigate(`/clients/${client.id}`)}>
                  Open
                </Button>
              </TableCell>
            </TableRow>
          ))}
          {clients.length === 0 ? (
            <TableRow>
              <TableCell colSpan={7}>No clients match this search.</TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Button
          disabled={filters.page <= 1}
          onClick={() => setFilters((current) => ({ ...current, page: current.page - 1 }))}
        >
          Previous
        </Button>
        <Typography variant="body2">
          Page {filters.page}
          {totalPages > 0 ? ` of ${totalPages}` : ''}
        </Typography>
        <Button
          disabled={totalPages === 0 || filters.page >= totalPages}
          onClick={() => setFilters((current) => ({ ...current, page: current.page + 1 }))}
        >
          Next
        </Button>
      </Stack>
      {creating ? (
        <ClientCreateDialog
          onClose={() => setCreating(false)}
          onCreated={(clientId) => {
            setCreating(false)
            setReloadKey((current) => current + 1)
            navigate(`/clients/${clientId}`)
          }}
        />
      ) : null}
    </Stack>
  )
}

function ClientCreateDialog({
  onClose,
  onCreated,
}: {
  onClose: () => void
  onCreated: (clientId: string) => void
}) {
  const [form, setForm] = useState(emptyForm)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)

  async function submit() {
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const created = await createClient({
        name: form.name,
        contactPerson: form.contactPerson || undefined,
        phone: form.phone || undefined,
        email: form.email || undefined,
        address: form.address || undefined,
        notes: form.notes || undefined,
      })
      onCreated(created.id)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        contactPerson: fieldMessage(caught, 'contactPerson'),
        phone: fieldMessage(caught, 'phone'),
        email: fieldMessage(caught, 'email'),
      })
      setSaving(false)
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>New client</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          <TextField label="Client ID" value="Generated automatically" disabled />
          <TextField
            label="Name"
            required
            value={form.name}
            error={Boolean(fieldErrors.name)}
            helperText={fieldErrors.name}
            onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))}
          />
          <TextField
            label="Contact person"
            value={form.contactPerson}
            error={Boolean(fieldErrors.contactPerson)}
            helperText={fieldErrors.contactPerson}
            onChange={(event) =>
              setForm((current) => ({ ...current, contactPerson: event.target.value }))
            }
          />
          <TextField
            label="Phone"
            value={form.phone}
            error={Boolean(fieldErrors.phone)}
            helperText={fieldErrors.phone}
            onChange={(event) => setForm((current) => ({ ...current, phone: event.target.value }))}
          />
          <TextField
            label="Email"
            value={form.email}
            error={Boolean(fieldErrors.email)}
            helperText={fieldErrors.email}
            onChange={(event) => setForm((current) => ({ ...current, email: event.target.value }))}
          />
          <TextField
            label="Address"
            value={form.address}
            onChange={(event) =>
              setForm((current) => ({ ...current, address: event.target.value }))
            }
          />
          <TextField
            label="Notes"
            multiline
            minRows={2}
            value={form.notes}
            onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={saving} onClick={() => void submit()}>
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
