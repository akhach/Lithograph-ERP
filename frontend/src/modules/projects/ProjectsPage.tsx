import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
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
import { listActiveClientOptions, type ClientOption } from '../clients/clientsApi.ts'
import { listActiveEmployeeOptions, type EmployeeOption } from '../employees/employeesApi.ts'
import {
  createProject,
  listProjects,
  projectStatusLabels,
  type ProjectListItem,
  type ProjectStatus,
} from './projectsApi.ts'

type StatusFilter = 'open' | 'all' | ProjectStatus

type Filters = {
  search: string
  status: StatusFilter
  clientId: string
  ownerEmployeeId: string
  assigneeEmployeeId: string
  startFrom: string
  startTo: string
  deadlineFrom: string
  deadlineTo: string
  page: number
}

export function ProjectsPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const canCreate = auth.hasPermission(PermissionCodes.projectsCreate)
  const [filters, setFilters] = useState<Filters>({
    search: '',
    status: 'open',
    clientId: '',
    ownerEmployeeId: '',
    assigneeEmployeeId: '',
    startFrom: '',
    startTo: '',
    deadlineFrom: '',
    deadlineTo: '',
    page: 1,
  })
  const [draftSearch, setDraftSearch] = useState('')
  const [projects, setProjects] = useState<ProjectListItem[]>([])
  const [clients, setClients] = useState<ClientOption[]>([])
  const [employees, setEmployees] = useState<EmployeeOption[]>([])
  const [totalPages, setTotalPages] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [clientPage, employeePage] = await Promise.all([
          listActiveClientOptions(),
          listActiveEmployeeOptions(),
        ])
        if (!cancelled) {
          setClients(clientPage.items)
          setEmployees(employeePage.items)
        }
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const page = await listProjects({
          search: filters.search || undefined,
          status: filters.status === 'all' ? undefined : filters.status,
          clientId: filters.clientId || undefined,
          ownerEmployeeId: filters.ownerEmployeeId || undefined,
          assigneeEmployeeId: filters.assigneeEmployeeId || undefined,
          startFrom: filters.startFrom || undefined,
          startTo: filters.startTo || undefined,
          deadlineFrom: filters.deadlineFrom || undefined,
          deadlineTo: filters.deadlineTo || undefined,
          page: filters.page,
          sort: 'created_at',
          direction: 'desc',
        })
        if (cancelled) return
        setProjects(page.items)
        setTotalPages(page.totalPages)
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
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
          Projects
        </Typography>
        {canCreate ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create project
          </Button>
        ) : null}
      </Stack>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField
          label="Search projects"
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
              status: event.target.value as StatusFilter,
              page: 1,
            }))
          }
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="open">Open</MenuItem>
          <MenuItem value="all">All</MenuItem>
          {Object.entries(projectStatusLabels).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Client"
          size="small"
          value={filters.clientId}
          onChange={(event) =>
            setFilters((current) => ({ ...current, clientId: event.target.value, page: 1 }))
          }
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">All clients</MenuItem>
          {clients.map((client) => (
            <MenuItem key={client.id} value={client.id}>
              {client.businessId} — {client.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Owner"
          size="small"
          value={filters.ownerEmployeeId}
          onChange={(event) =>
            setFilters((current) => ({ ...current, ownerEmployeeId: event.target.value, page: 1 }))
          }
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Any owner</MenuItem>
          {employees.map((employee) => (
            <MenuItem key={employee.id} value={employee.id}>
              {employee.fullName}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Assignee"
          size="small"
          value={filters.assigneeEmployeeId}
          onChange={(event) =>
            setFilters((current) => ({
              ...current,
              assigneeEmployeeId: event.target.value,
              page: 1,
            }))
          }
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">Any assignee</MenuItem>
          {employees.map((employee) => (
            <MenuItem key={employee.id} value={employee.id}>
              {employee.fullName}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Start from"
          type="date"
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          value={filters.startFrom}
          onChange={(event) =>
            setFilters((current) => ({ ...current, startFrom: event.target.value, page: 1 }))
          }
        />
        <TextField
          label="Start to"
          type="date"
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          value={filters.startTo}
          onChange={(event) =>
            setFilters((current) => ({ ...current, startTo: event.target.value, page: 1 }))
          }
        />
        <TextField
          label="Deadline from"
          type="date"
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          value={filters.deadlineFrom}
          onChange={(event) =>
            setFilters((current) => ({ ...current, deadlineFrom: event.target.value, page: 1 }))
          }
        />
        <TextField
          label="Deadline to"
          type="date"
          size="small"
          slotProps={{ inputLabel: { shrink: true } }}
          value={filters.deadlineTo}
          onChange={(event) =>
            setFilters((current) => ({ ...current, deadlineTo: event.target.value, page: 1 }))
          }
        />
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Project ID</TableCell>
            <TableCell>Project</TableCell>
            <TableCell>Client</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Deadline</TableCell>
            <TableCell>Owner</TableCell>
            <TableCell>Assignee</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {projects.map((project) => (
            <TableRow
              key={project.id}
              hover
              onDoubleClick={() => navigate(`/projects/${project.id}`)}
              sx={{ cursor: 'pointer' }}
            >
              <TableCell>{project.businessId}</TableCell>
              <TableCell>{project.name}</TableCell>
              <TableCell>{project.client.name}</TableCell>
              <TableCell>
                <Chip size="small" label={projectStatusLabels[project.status]} />
              </TableCell>
              <TableCell>{project.deadline ?? ''}</TableCell>
              <TableCell>{personLabel(project.owner)}</TableCell>
              <TableCell>{personLabel(project.assignee)}</TableCell>
              <TableCell>
                <Button size="small" onClick={() => navigate(`/projects/${project.id}`)}>
                  Open
                </Button>
              </TableCell>
            </TableRow>
          ))}
          {projects.length === 0 ? (
            <TableRow>
              <TableCell colSpan={8}>No projects match this search.</TableCell>
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
        <ProjectCreateDialog
          clients={clients}
          onClose={() => setCreating(false)}
          onCreated={(projectId) => {
            setCreating(false)
            setReloadKey((current) => current + 1)
            navigate(`/projects/${projectId}`)
          }}
        />
      ) : null}
    </Stack>
  )
}

function personLabel(person: ProjectListItem['owner']): string {
  if (!person) return ''
  return person.isActive ? person.fullName : `${person.fullName} — Inactive`
}

function ProjectCreateDialog({
  clients,
  onClose,
  onCreated,
}: {
  clients: ClientOption[]
  onClose: () => void
  onCreated: (projectId: string) => void
}) {
  const [clientId, setClientId] = useState(clients[0]?.id ?? '')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [startDate, setStartDate] = useState('')
  const [deadline, setDeadline] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)

  async function submit() {
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const created = await createProject({
        clientId,
        name,
        description: description || undefined,
        startDate: startDate || undefined,
        deadline: deadline || undefined,
      })
      onCreated(created.id)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        deadline: fieldMessage(caught, 'deadline'),
      })
      setSaving(false)
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>New project</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          <TextField label="Project ID" value="Generated automatically" disabled />
          <TextField
            select
            label="Client"
            required
            value={clientId}
            onChange={(event) => setClientId(event.target.value)}
          >
            {clients.map((client) => (
              <MenuItem key={client.id} value={client.id}>
                {client.businessId} — {client.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            label="Name"
            required
            value={name}
            error={Boolean(fieldErrors.name)}
            helperText={fieldErrors.name}
            onChange={(event) => setName(event.target.value)}
          />
          <TextField
            label="Description"
            multiline
            minRows={2}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
          />
          <TextField
            label="Start date"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={startDate}
            onChange={(event) => setStartDate(event.target.value)}
          />
          <TextField
            label="Deadline"
            type="date"
            slotProps={{ inputLabel: { shrink: true } }}
            value={deadline}
            error={Boolean(fieldErrors.deadline)}
            helperText={fieldErrors.deadline}
            onChange={(event) => setDeadline(event.target.value)}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" disabled={saving || !clientId} onClick={() => void submit()}>
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
