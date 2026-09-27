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
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  activateEmployee,
  createEmployee,
  deactivateEmployee,
  linkEmployeeUser,
  listAvailableUsers,
  listEmployees,
  unlinkEmployeeUser,
  updateEmployee,
  type Employee,
  type LinkedUser,
} from './employeesApi.ts'

type Filters = {
  search: string
  status: '' | 'active' | 'inactive'
  user: '' | 'linked' | 'unlinked'
  page: number
}

export function EmployeesPage() {
  const auth = useAuth()
  const canCreate = auth.hasPermission(PermissionCodes.employeesCreate)
  const canEdit = auth.hasPermission(PermissionCodes.employeesEdit)
  const canActivate = auth.hasPermission(PermissionCodes.employeesActivate)
  const canLink = auth.hasPermission(PermissionCodes.employeesLinkUser)
  const [filters, setFilters] = useState<Filters>({ search: '', status: '', user: '', page: 1 })
  const [draftSearch, setDraftSearch] = useState('')
  const [employees, setEmployees] = useState<Employee[]>([])
  const [totalPages, setTotalPages] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState<Employee | null>(null)
  const [deactivating, setDeactivating] = useState<Employee | null>(null)
  const [linking, setLinking] = useState<Employee | null>(null)

  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const page = await listEmployees({
          search: filters.search || undefined,
          isActive: filters.status === '' ? undefined : filters.status === 'active',
          hasUser: filters.user === '' ? undefined : filters.user === 'linked',
          page: filters.page,
          sort: 'full_name',
        })
        if (cancelled) {
          return
        }
        setEmployees(page.items)
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

  function reload() {
    setReloadKey((current) => current + 1)
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Employees
        </Typography>
        {canCreate ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create employee
          </Button>
        ) : null}
      </Stack>
      <Stack
        component="form"
        direction={{ xs: 'column', md: 'row' }}
        spacing={1}
        onSubmit={(event) => {
          event.preventDefault()
          setFilters({ ...filters, search: draftSearch, page: 1 })
        }}
      >
        <TextField
          label="Search"
          value={draftSearch}
          onChange={(event) => setDraftSearch(event.target.value)}
          placeholder="Name, position, phone, email, or username"
        />
        <TextField
          select
          label="Status"
          value={filters.status}
          onChange={(event) =>
            void setFilters({
              ...filters,
              status: event.target.value as Filters['status'],
              page: 1,
            })
          }
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="">All</MenuItem>
          <MenuItem value="active">Active</MenuItem>
          <MenuItem value="inactive">Inactive</MenuItem>
        </TextField>
        <TextField
          select
          label="User account"
          value={filters.user}
          onChange={(event) =>
            void setFilters({ ...filters, user: event.target.value as Filters['user'], page: 1 })
          }
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">All</MenuItem>
          <MenuItem value="linked">Has user</MenuItem>
          <MenuItem value="unlinked">No user</MenuItem>
        </TextField>
        <Button type="submit" variant="outlined">
          Search
        </Button>
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Full name</TableCell>
            <TableCell>Position</TableCell>
            <TableCell>Phone</TableCell>
            <TableCell>Email</TableCell>
            <TableCell>Linked user</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {employees.map((employee) => (
            <TableRow key={employee.id}>
              <TableCell>{employee.fullName}</TableCell>
              <TableCell>{employee.position || '—'}</TableCell>
              <TableCell>{employee.phone || '—'}</TableCell>
              <TableCell>{employee.email || '—'}</TableCell>
              <TableCell>{userLabel(employee.linkedUser)}</TableCell>
              <TableCell>{employee.isActive ? 'Active' : 'Inactive'}</TableCell>
              <TableCell>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  {canEdit ? (
                    <Button size="small" onClick={() => setEditing(employee)}>
                      Edit
                    </Button>
                  ) : null}
                  {canActivate && employee.isActive ? (
                    <Button size="small" onClick={() => setDeactivating(employee)}>
                      Deactivate
                    </Button>
                  ) : null}
                  {canActivate && !employee.isActive ? (
                    <Button
                      size="small"
                      onClick={() =>
                        void run(() => activateEmployee(employee.id), reload, setError)
                      }
                    >
                      Activate
                    </Button>
                  ) : null}
                  {canLink && !employee.linkedUser ? (
                    <Button size="small" onClick={() => setLinking(employee)}>
                      Link user
                    </Button>
                  ) : null}
                  {canLink && employee.linkedUser ? (
                    <Button
                      size="small"
                      onClick={() =>
                        void run(() => unlinkEmployeeUser(employee.id), reload, setError)
                      }
                    >
                      Unlink user
                    </Button>
                  ) : null}
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          {employees.length === 0 ? (
            <TableRow>
              <TableCell colSpan={7}>No employees.</TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>
      {totalPages > 1 ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Button
            disabled={filters.page <= 1}
            onClick={() => setFilters({ ...filters, page: filters.page - 1 })}
          >
            Previous
          </Button>
          <Typography>
            Page {filters.page} of {totalPages}
          </Typography>
          <Button
            disabled={filters.page >= totalPages}
            onClick={() => setFilters({ ...filters, page: filters.page + 1 })}
          >
            Next
          </Button>
        </Stack>
      ) : null}
      <EmployeeFormDialog
        open={creating}
        title="Create employee"
        canLink={canLink}
        employee={null}
        onClose={() => setCreating(false)}
        onSaved={() => {
          setCreating(false)
          reload()
        }}
      />
      <EmployeeFormDialog
        key={editing?.id ?? 'edit-closed'}
        open={editing !== null}
        title="Edit employee"
        canLink={false}
        employee={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          reload()
        }}
      />
      <DeactivateEmployeeDialog
        employee={deactivating}
        onClose={() => setDeactivating(null)}
        onDeactivated={() => {
          setDeactivating(null)
          reload()
        }}
      />
      <LinkUserDialog
        key={linking?.id ?? 'link-closed'}
        employee={linking}
        onClose={() => setLinking(null)}
        onLinked={() => {
          setLinking(null)
          reload()
        }}
      />
    </Stack>
  )
}

function EmployeeFormDialog({
  open,
  title,
  canLink,
  employee,
  onClose,
  onSaved,
}: {
  open: boolean
  title: string
  canLink: boolean
  employee: Employee | null
  onClose: () => void
  onSaved: () => void
}) {
  const [fullName, setFullName] = useState(employee?.fullName ?? '')
  const [position, setPosition] = useState(employee?.position ?? '')
  const [phone, setPhone] = useState(employee?.phone ?? '')
  const [email, setEmail] = useState(employee?.email ?? '')
  const [userId, setUserId] = useState('')
  const [users, setUsers] = useState<LinkedUser[]>([])
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!open || !canLink || employee) {
      return
    }
    let cancelled = false
    void listAvailableUsers()
      .then((next) => {
        if (!cancelled) {
          setUsers(next)
        }
      })
      .catch((caught: unknown) => {
        if (!cancelled) {
          setError(caught)
        }
      })
    return () => {
      cancelled = true
    }
  }, [open, canLink, employee])

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      if (employee) {
        await updateEmployee(employee.id, { fullName, position, phone, email })
      } else {
        await createEmployee({
          fullName,
          position,
          phone,
          email,
          userId: canLink && userId ? userId : undefined,
        })
      }
      onSaved()
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>{title}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Full name"
              value={fullName}
              onChange={(event) => setFullName(event.target.value)}
              error={Boolean(fieldMessage(error, 'fullName'))}
              helperText={fieldMessage(error, 'fullName')}
              required
            />
            <TextField
              label="Position"
              value={position}
              onChange={(event) => setPosition(event.target.value)}
            />
            <TextField
              label="Phone"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
            />
            <TextField
              label="Email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
            {canLink && !employee ? (
              <TextField
                select
                label="User account"
                value={userId}
                onChange={(event) => setUserId(event.target.value)}
              >
                <MenuItem value="">None</MenuItem>
                {users.map((user) => (
                  <MenuItem key={user.id} value={user.id}>
                    {user.username} — {user.isActive ? 'Active' : 'Inactive'}
                  </MenuItem>
                ))}
              </TextField>
            ) : null}
            {error && !fieldMessage(error, 'fullName') ? (
              <Alert severity="error">{messageOf(error)}</Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            {employee ? 'Save' : 'Create'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function DeactivateEmployeeDialog({
  employee,
  onClose,
  onDeactivated,
}: {
  employee: Employee | null
  onClose: () => void
  onDeactivated: () => void
}) {
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const linkedActive = Boolean(employee?.linkedUser?.isActive)

  return (
    <Dialog open={employee !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Deactivate employee?</DialogTitle>
      <DialogContent>
        <Stack spacing={1}>
          <Typography>
            {linkedActive
              ? 'This Employee has an active ERP User account. Deactivating the Employee does not disable ERP login access.'
              : 'The Employee will remain stored and will not be offered for new assignments.'}
          </Typography>
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          color="warning"
          variant="contained"
          disabled={submitting || !employee}
          onClick={() => {
            if (!employee) {
              return
            }
            setSubmitting(true)
            void deactivateEmployee(employee.id)
              .then(() => onDeactivated())
              .catch((caught: unknown) => setError(messageOf(caught)))
              .finally(() => setSubmitting(false))
          }}
        >
          Deactivate
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function LinkUserDialog({
  employee,
  onClose,
  onLinked,
}: {
  employee: Employee | null
  onClose: () => void
  onLinked: () => void
}) {
  const [users, setUsers] = useState<LinkedUser[]>([])
  const [userId, setUserId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    if (!employee) {
      return
    }
    let cancelled = false
    void listAvailableUsers()
      .then((next) => {
        if (!cancelled) {
          setUsers(next)
        }
      })
      .catch((caught: unknown) => {
        if (!cancelled) {
          setError(messageOf(caught))
        }
      })
    return () => {
      cancelled = true
    }
  }, [employee])

  return (
    <Dialog open={employee !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Link user</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ pt: 1 }}>
          <TextField
            select
            label="User account"
            value={userId}
            onChange={(event) => setUserId(event.target.value)}
          >
            <MenuItem value="">None</MenuItem>
            {users.map((user) => (
              <MenuItem key={user.id} value={user.id}>
                {user.username} — {user.isActive ? 'Active' : 'Inactive'}
              </MenuItem>
            ))}
          </TextField>
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={submitting || !employee || !userId}
          onClick={() => {
            if (!employee || !userId) {
              return
            }
            setSubmitting(true)
            void linkEmployeeUser(employee.id, userId)
              .then(() => onLinked())
              .catch((caught: unknown) => setError(messageOf(caught)))
              .finally(() => setSubmitting(false))
          }}
        >
          Link
        </Button>
      </DialogActions>
    </Dialog>
  )
}

function userLabel(user: LinkedUser | null): string {
  if (!user) {
    return '—'
  }
  return `${user.username} (${user.isActive ? 'Active' : 'Inactive'})`
}

async function run(
  action: () => Promise<unknown>,
  reload: () => void,
  setError: (message: string | null) => void,
) {
  try {
    await action()
    reload()
  } catch (caught: unknown) {
    setError(messageOf(caught))
  }
}
