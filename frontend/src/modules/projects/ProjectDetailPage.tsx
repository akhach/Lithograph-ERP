import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  clientOptionLabel,
  listActiveClientOptions,
  type ClientOption,
} from '../clients/clientsApi.ts'
import {
  employeeOptionLabel,
  listActiveEmployeeOptions,
  type EmployeeOption,
} from '../employees/employeesApi.ts'
import {
  addProjectObserver,
  addProjectParticipant,
  changeProjectStatus,
  clearProjectAssignee,
  clearProjectOwner,
  getProject,
  projectStatusLabels,
  removeProjectObserver,
  removeProjectParticipant,
  setProjectAssignee,
  setProjectOwner,
  updateProject,
  type Project,
  type ProjectPerson,
  type ProjectStatus,
} from './projectsApi.ts'

export function ProjectDetailPage() {
  const auth = useAuth()
  const { projectId = '' } = useParams()
  const canEdit = auth.hasPermission(PermissionCodes.projectsEdit)
  const canChangeStatus = auth.hasPermission(PermissionCodes.projectsChangeStatus)
  const canManageTeam = auth.hasPermission(PermissionCodes.projectsManageTeam)
  const [project, setProject] = useState<Project | null>(null)
  const [clients, setClients] = useState<ClientOption[]>([])
  const [employees, setEmployees] = useState<EmployeeOption[]>([])
  const [status, setStatus] = useState<ProjectStatus>('draft')
  const [ownerId, setOwnerId] = useState('')
  const [assigneeId, setAssigneeId] = useState('')
  const [participantId, setParticipantId] = useState('')
  const [observerId, setObserverId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [loaded, clientPage, employeePage] = await Promise.all([
          getProject(projectId),
          listActiveClientOptions(),
          listActiveEmployeeOptions(),
        ])
        if (cancelled) return
        setProject(loaded)
        setStatus(loaded.status)
        setClients(clientPage.items)
        setEmployees(employeePage.items)
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) {
          setProject(null)
          setError(messageOf(caught))
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [projectId])

  async function save() {
    if (!project) return
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      setProject(
        await updateProject(project.id, {
          clientId: project.client.id,
          name: project.name,
          description: project.description ?? '',
          startDate: project.startDate ?? '',
          deadline: project.deadline ?? '',
        }),
      )
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        deadline: fieldMessage(caught, 'deadline'),
      })
    } finally {
      setSaving(false)
    }
  }

  async function applyStatus() {
    if (!project) return
    try {
      const updated = await changeProjectStatus(project.id, status)
      setProject(updated)
      setStatus(updated.status)
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  async function run(action: () => Promise<Project>) {
    try {
      setProject(await action())
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  if (!project) {
    return (
      <Stack spacing={2}>
        <Button component={Link} to="/projects">
          Back to projects
        </Button>
        {error ? (
          <Alert severity="error">{error}</Alert>
        ) : (
          <Typography>Loading project…</Typography>
        )}
      </Stack>
    )
  }

  const clientChoices = clients.some((client) => client.id === project.client.id)
    ? clients
    : [
        { id: project.client.id, businessId: project.client.businessId, name: project.client.name },
        ...clients,
      ]

  return (
    <Stack spacing={3} sx={{ maxWidth: 800 }}>
      <Button component={Link} to="/projects" sx={{ alignSelf: 'flex-start' }}>
        Back to projects
      </Button>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          {project.businessId}
        </Typography>
        <Button size="small" onClick={() => void navigator.clipboard.writeText(project.businessId)}>
          Copy
        </Button>
        <Chip label={projectStatusLabels[project.status]} />
        {auth.hasPermission(PermissionCodes.ordersView) &&
        auth.hasPermission(PermissionCodes.ordersCreate) &&
        (project.status === 'draft' ||
          project.status === 'active' ||
          project.status === 'on_hold') ? (
          <Button
            component={Link}
            to={`/orders?projectId=${project.id}`}
            size="small"
            variant="outlined"
          >
            Create order
          </Button>
        ) : null}
      </Stack>
      <Typography variant="h6">{project.name}</Typography>
      <Typography>
        Client:{' '}
        <Link to={`/clients/${project.client.id}`}>
          {project.client.businessId} — {project.client.name}
          {project.client.isActive ? '' : ' — Inactive'}
        </Link>
      </Typography>
      <Typography>Deadline: {project.deadline ?? 'Not set'}</Typography>
      {error ? <Alert severity="error">{error}</Alert> : null}

      <Typography variant="h6" component="h3">
        General information
      </Typography>
      <TextField
        select
        label="Client"
        disabled={!canEdit}
        value={project.client.id}
        onChange={(event) => {
          const selected = clientChoices.find((client) => client.id === event.target.value)
          if (selected) {
            setProject({
              ...project,
              client: {
                ...project.client,
                id: selected.id,
                businessId: selected.businessId,
                name: selected.name,
              },
            })
          }
        }}
      >
        {clientChoices.map((client) => (
          <MenuItem key={client.id} value={client.id}>
            {clientOptionLabel(client)}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        label="Name"
        required
        disabled={!canEdit}
        value={project.name}
        error={Boolean(fieldErrors.name)}
        helperText={fieldErrors.name}
        onChange={(event) => setProject({ ...project, name: event.target.value })}
      />
      <TextField
        label="Description"
        disabled={!canEdit}
        multiline
        minRows={3}
        value={project.description ?? ''}
        onChange={(event) => setProject({ ...project, description: event.target.value })}
      />
      <TextField
        label="Start date"
        type="date"
        disabled={!canEdit}
        slotProps={{ inputLabel: { shrink: true } }}
        value={project.startDate ?? ''}
        onChange={(event) => setProject({ ...project, startDate: event.target.value || null })}
      />
      <TextField
        label="Deadline"
        type="date"
        disabled={!canEdit}
        slotProps={{ inputLabel: { shrink: true } }}
        value={project.deadline ?? ''}
        error={Boolean(fieldErrors.deadline)}
        helperText={fieldErrors.deadline}
        onChange={(event) => setProject({ ...project, deadline: event.target.value || null })}
      />
      {canEdit ? (
        <Button
          variant="contained"
          disabled={saving}
          sx={{ alignSelf: 'flex-start' }}
          onClick={() => void save()}
        >
          Save
        </Button>
      ) : null}

      <Typography variant="h6" component="h3">
        Status
      </Typography>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <TextField
          select
          label="Status"
          size="small"
          disabled={!canChangeStatus}
          value={status}
          onChange={(event) => setStatus(event.target.value as ProjectStatus)}
          sx={{ minWidth: 180 }}
        >
          {Object.entries(projectStatusLabels).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        {canChangeStatus ? <Button onClick={() => void applyStatus()}>Change status</Button> : null}
      </Stack>

      <Typography variant="h6" component="h3">
        Project team
      </Typography>
      <TeamRole
        label="Owner"
        person={project.team.owner}
        employeeId={ownerId}
        employees={employees}
        canManage={canManageTeam}
        onSelect={setOwnerId}
        onAssign={() => void run(() => setProjectOwner(project.id, ownerId))}
        onClear={() => void run(() => clearProjectOwner(project.id))}
      />
      <TeamRole
        label="Assignee"
        person={project.team.assignee}
        employeeId={assigneeId}
        employees={employees}
        canManage={canManageTeam}
        onSelect={setAssigneeId}
        onAssign={() => void run(() => setProjectAssignee(project.id, assigneeId))}
        onClear={() => void run(() => clearProjectAssignee(project.id))}
      />
      <MemberList
        label="Participants"
        people={project.team.participants}
        employeeId={participantId}
        employees={employees}
        canManage={canManageTeam}
        onSelect={setParticipantId}
        onAdd={() => void run(() => addProjectParticipant(project.id, participantId))}
        onRemove={(employeeId) => void run(() => removeProjectParticipant(project.id, employeeId))}
      />
      <MemberList
        label="Observers"
        people={project.team.observers}
        employeeId={observerId}
        employees={employees}
        canManage={canManageTeam}
        onSelect={setObserverId}
        onAdd={() => void run(() => addProjectObserver(project.id, observerId))}
        onRemove={(employeeId) => void run(() => removeProjectObserver(project.id, employeeId))}
      />

      <Typography variant="h6" component="h3">
        Orders
      </Typography>
      <Typography color="text.secondary">No Orders yet.</Typography>
    </Stack>
  )
}

function TeamRole({
  label,
  person,
  employeeId,
  employees,
  canManage,
  onSelect,
  onAssign,
  onClear,
}: {
  label: string
  person: ProjectPerson | null
  employeeId: string
  employees: EmployeeOption[]
  canManage: boolean
  onSelect: (employeeId: string) => void
  onAssign: () => void
  onClear: () => void
}) {
  return (
    <Stack spacing={1}>
      <Typography>
        {label}: {person ? personText(person) : 'Not assigned'}
      </Typography>
      {canManage ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <TextField
            select
            label={`Set ${label.toLowerCase()}`}
            size="small"
            value={employeeId}
            onChange={(event) => onSelect(event.target.value)}
            sx={{ minWidth: 240 }}
          >
            {employees.map((employee) => (
              <MenuItem key={employee.id} value={employee.id}>
                {employeeOptionLabel(employee)}
              </MenuItem>
            ))}
          </TextField>
          <Button disabled={!employeeId} onClick={onAssign}>
            Assign
          </Button>
          <Button disabled={!person} onClick={onClear}>
            Clear
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )
}

function MemberList({
  label,
  people,
  employeeId,
  employees,
  canManage,
  onSelect,
  onAdd,
  onRemove,
}: {
  label: string
  people: ProjectPerson[]
  employeeId: string
  employees: EmployeeOption[]
  canManage: boolean
  onSelect: (employeeId: string) => void
  onAdd: () => void
  onRemove: (employeeId: string) => void
}) {
  return (
    <Stack spacing={1}>
      <Typography>{label}</Typography>
      {people.length === 0 ? <Typography color="text.secondary">None</Typography> : null}
      {people.map((person) => (
        <Stack key={person.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Typography>{personText(person)}</Typography>
          {canManage ? (
            <Button size="small" onClick={() => onRemove(person.id)}>
              Remove
            </Button>
          ) : null}
        </Stack>
      ))}
      {canManage ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <TextField
            select
            label={`Add ${label.slice(0, -1).toLowerCase()}`}
            size="small"
            value={employeeId}
            onChange={(event) => onSelect(event.target.value)}
            sx={{ minWidth: 240 }}
          >
            {employees.map((employee) => (
              <MenuItem key={employee.id} value={employee.id}>
                {employeeOptionLabel(employee)}
              </MenuItem>
            ))}
          </TextField>
          <Button disabled={!employeeId} onClick={onAdd}>
            Add
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )
}

function personText(person: ProjectPerson): string {
  const name = person.position ? `${person.fullName} — ${person.position}` : person.fullName
  return person.isActive ? name : `${name} — Inactive`
}
