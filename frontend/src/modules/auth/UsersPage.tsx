import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import FormControlLabel from '@mui/material/FormControlLabel'
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
import { useAuth } from '../auth/authContext.ts'
import {
  activateUser,
  assignRole,
  createUser,
  deactivateUser,
  listRoles,
  listUsers,
  removeRole,
  resetPassword,
  updateUsername,
} from '../auth/authApi.ts'
import { fieldMessage, messageOf, passwordsMatch } from '../auth/apiMessages.ts'
import { PermissionCodes, type Role, type UserAccount } from '../auth/authTypes.ts'

export function UsersPage() {
  const auth = useAuth()
  const [users, setUsers] = useState<UserAccount[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState<UserAccount | null>(null)
  const [resetting, setResetting] = useState<UserAccount | null>(null)
  const [deactivating, setDeactivating] = useState<UserAccount | null>(null)
  const [managing, setManaging] = useState<UserAccount | null>(null)

  const canCreate = auth.hasPermission(PermissionCodes.usersCreate)
  const canEdit = auth.hasPermission(PermissionCodes.usersEdit)
  const canActivate = auth.hasPermission(PermissionCodes.usersActivate)
  const canReset = auth.hasPermission(PermissionCodes.usersResetPassword)
  const canManageRoles = auth.hasPermission(PermissionCodes.usersManageRoles)
  const canViewRoles = auth.hasPermission(PermissionCodes.rolesView)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const [nextUsers, nextRoles] = await Promise.all([
        listUsers(),
        canViewRoles ? listRoles() : Promise.resolve([]),
      ])
      setUsers(nextUsers)
      setRoles(nextRoles)
      setManaging((current) =>
        current ? (nextUsers.find((user) => user.id === current.id) ?? null) : null,
      )
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [nextUsers, nextRoles] = await Promise.all([
          listUsers(),
          canViewRoles ? listRoles() : Promise.resolve([] as Role[]),
        ])
        if (cancelled) {
          return
        }
        setUsers(nextUsers)
        setRoles(nextRoles)
      } catch (caught: unknown) {
        if (!cancelled) {
          setError(messageOf(caught))
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [canViewRoles])

  const activeDirectorCount = users.filter(
    (user) => user.isActive && user.roles.some((role) => role.isSystem),
  ).length

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Users
        </Typography>
        {canCreate ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create user
          </Button>
        ) : null}
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Username</TableCell>
            <TableCell>Roles</TableCell>
            <TableCell>Status</TableCell>
            <TableCell>Last login</TableCell>
            <TableCell>Actions</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {users.map((user) => {
            const finalDirector =
              user.isActive && user.roles.some((role) => role.isSystem) && activeDirectorCount <= 1
            return (
              <TableRow key={user.id}>
                <TableCell>{user.username}</TableCell>
                <TableCell>{user.roles.map((role) => role.name).join(', ') || '—'}</TableCell>
                <TableCell>{user.isActive ? 'Active' : 'Inactive'}</TableCell>
                <TableCell>{formatLogin(user.lastLoginAt)}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                    {canEdit ? (
                      <Button size="small" onClick={() => setEditing(user)}>
                        Edit
                      </Button>
                    ) : null}
                    {canActivate && user.isActive ? (
                      <span
                        title={
                          finalDirector ? 'At least one active Director is required.' : undefined
                        }
                      >
                        <Button
                          size="small"
                          disabled={finalDirector}
                          onClick={() => setDeactivating(user)}
                        >
                          Deactivate
                        </Button>
                      </span>
                    ) : null}
                    {canActivate && !user.isActive ? (
                      <Button
                        size="small"
                        onClick={() => void run(() => activateUser(user.id), load, setError)}
                      >
                        Activate
                      </Button>
                    ) : null}
                    {canReset ? (
                      <Button size="small" onClick={() => setResetting(user)}>
                        Reset password
                      </Button>
                    ) : null}
                    {canManageRoles ? (
                      <Button size="small" onClick={() => setManaging(user)}>
                        Manage roles
                      </Button>
                    ) : null}
                  </Stack>
                </TableCell>
              </TableRow>
            )
          })}
          {!loading && users.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5}>No users.</TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>

      <CreateUserDialog
        open={creating}
        roles={canManageRoles ? roles : []}
        canAssignRoles={canManageRoles}
        onClose={() => setCreating(false)}
        onCreated={() => {
          setCreating(false)
          void load()
        }}
      />
      <EditUserDialog
        key={editing?.id ?? 'edit-closed'}
        user={editing}
        onClose={() => setEditing(null)}
        onSaved={() => {
          setEditing(null)
          void load()
        }}
      />
      <ResetPasswordDialog
        key={resetting?.id ?? 'reset-closed'}
        user={resetting}
        onClose={() => setResetting(null)}
      />
      <DeactivateDialog
        key={deactivating?.id ?? 'deactivate-closed'}
        user={deactivating}
        onClose={() => setDeactivating(null)}
        onDeactivated={() => {
          setDeactivating(null)
          void load()
        }}
      />
      <ManageRolesDialog
        user={managing}
        roles={roles}
        activeDirectorCount={activeDirectorCount}
        onClose={() => setManaging(null)}
        onChanged={async () => {
          await load()
          await auth.reloadUser()
        }}
      />
    </Stack>
  )
}

function CreateUserDialog({
  open,
  roles,
  canAssignRoles,
  onClose,
  onCreated,
}: {
  open: boolean
  roles: Role[]
  canAssignRoles: boolean
  onClose: () => void
  onCreated: () => void
}) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [roleIds, setRoleIds] = useState<string[]>([])
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  function close() {
    setUsername('')
    setPassword('')
    setConfirmation('')
    setRoleIds([])
    setError(null)
    onClose()
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    const confirmationError = passwordsMatch(password, confirmation)
    if (confirmationError) {
      setError(new Error(confirmationError))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await createUser({
        username,
        password,
        roleIds: canAssignRoles && roleIds.length > 0 ? roleIds : undefined,
      })
      setUsername('')
      setPassword('')
      setConfirmation('')
      setRoleIds([])
      onCreated()
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>Create user</DialogTitle>
        <DialogContent>
          <Stack spacing={1} sx={{ pt: 1 }}>
            <TextField
              label="Username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              error={Boolean(fieldMessage(error, 'username'))}
              helperText={fieldMessage(error, 'username')}
              required
            />
            <TextField
              label="Password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              error={Boolean(fieldMessage(error, 'password'))}
              helperText={fieldMessage(error, 'password')}
              required
            />
            <TextField
              label="Confirm password"
              type="password"
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              required
            />
            {canAssignRoles
              ? roles.map((role) => (
                  <FormControlLabel
                    key={role.id}
                    control={
                      <Checkbox
                        checked={roleIds.includes(role.id)}
                        onChange={(event) =>
                          setRoleIds((current) =>
                            event.target.checked
                              ? [...current, role.id]
                              : current.filter((id) => id !== role.id),
                          )
                        }
                      />
                    }
                    label={role.name}
                  />
                ))
              : null}
            {error && !fieldMessage(error, 'username') && !fieldMessage(error, 'password') ? (
              <Alert severity="error">{messageOf(error)}</Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={close}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            Create
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function EditUserDialog({
  user,
  onClose,
  onSaved,
}: {
  user: UserAccount | null
  onClose: () => void
  onSaved: () => void
}) {
  const [username, setUsername] = useState(user?.username ?? '')
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!user) {
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await updateUsername(user.id, username)
      onSaved()
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={user !== null} onClose={onClose} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>Edit user</DialogTitle>
        <DialogContent>
          <Stack spacing={1} sx={{ pt: 1 }}>
            <TextField
              label="Username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              error={Boolean(fieldMessage(error, 'username'))}
              helperText={fieldMessage(error, 'username')}
              required
            />
            {error && !fieldMessage(error, 'username') ? (
              <Alert severity="error">{messageOf(error)}</Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Cancel</Button>
          <Button type="submit" variant="contained" disabled={submitting}>
            Save
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function ResetPasswordDialog({ user, onClose }: { user: UserAccount | null; onClose: () => void }) {
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [done, setDone] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!user) {
      return
    }
    const confirmationError = passwordsMatch(password, confirmation)
    if (confirmationError) {
      setError(new Error(confirmationError))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await resetPassword(user.id, password)
      setDone(true)
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={user !== null} onClose={onClose} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>Reset password</DialogTitle>
        <DialogContent>
          <Stack spacing={1} sx={{ pt: 1 }}>
            <Typography>Set a new password for {user?.username}.</Typography>
            <TextField
              label="New password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              error={Boolean(fieldMessage(error, 'newPassword'))}
              helperText={fieldMessage(error, 'newPassword')}
              required
            />
            <TextField
              label="Confirm password"
              type="password"
              value={confirmation}
              onChange={(event) => setConfirmation(event.target.value)}
              required
            />
            {done ? (
              <Alert severity="success">
                Password reset. Existing sessions for this user were signed out.
              </Alert>
            ) : null}
            {error && !fieldMessage(error, 'newPassword') ? (
              <Alert severity="error">{messageOf(error)}</Alert>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Close</Button>
          <Button type="submit" variant="contained" disabled={submitting || done}>
            Reset password
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}

function DeactivateDialog({
  user,
  onClose,
  onDeactivated,
}: {
  user: UserAccount | null
  onClose: () => void
  onDeactivated: () => void
}) {
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  return (
    <Dialog open={user !== null} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>Deactivate user?</DialogTitle>
      <DialogContent>
        <Stack spacing={1}>
          <Typography>The user will no longer be able to access the ERP.</Typography>
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          color="warning"
          variant="contained"
          disabled={submitting}
          onClick={() => {
            if (!user) {
              return
            }
            setSubmitting(true)
            void deactivateUser(user.id)
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

function ManageRolesDialog({
  user,
  roles,
  activeDirectorCount,
  onClose,
  onChanged,
}: {
  user: UserAccount | null
  roles: Role[]
  activeDirectorCount: number
  onClose: () => void
  onChanged: () => Promise<void>
}) {
  const [roleId, setRoleId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const assigned = new Set(user?.roles.map((role) => role.id) ?? [])
  const available = roles.filter((role) => !assigned.has(role.id))

  async function change(action: () => Promise<unknown>) {
    setBusy(true)
    setError(null)
    try {
      await action()
      setRoleId('')
      await onChanged()
    } catch (caught: unknown) {
      setError(messageOf(caught))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Dialog open={user !== null} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>Roles for {user?.username}</DialogTitle>
      <DialogContent>
        <Stack spacing={1} sx={{ pt: 1 }}>
          {user?.roles.map((role) => {
            const finalDirector = role.isSystem && user.isActive && activeDirectorCount <= 1
            return (
              <Stack
                key={role.id}
                direction="row"
                sx={{ justifyContent: 'space-between', alignItems: 'center' }}
              >
                <Typography>{role.name}</Typography>
                <span
                  title={finalDirector ? 'At least one active Director is required.' : undefined}
                >
                  <Button
                    size="small"
                    disabled={busy || finalDirector}
                    onClick={() => void change(() => removeRole(user.id, role.id))}
                  >
                    Remove
                  </Button>
                </span>
              </Stack>
            )
          })}
          {user && user.roles.length === 0 ? <Typography>No roles assigned.</Typography> : null}
          {available.length > 0 ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <TextField
                select
                label="Add role"
                value={roleId}
                onChange={(event) => setRoleId(event.target.value)}
                sx={{ minWidth: 200 }}
              >
                {available.map((role) => (
                  <MenuItem key={role.id} value={role.id}>
                    {role.name}
                  </MenuItem>
                ))}
              </TextField>
              <Button
                variant="contained"
                disabled={busy || !roleId || !user}
                onClick={() => {
                  if (user && roleId) {
                    void change(() => assignRole(user.id, roleId))
                  }
                }}
              >
                Add
              </Button>
            </Stack>
          ) : null}
          {error ? <Alert severity="error">{error}</Alert> : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  )
}

function formatLogin(value: string | null): string {
  if (!value) {
    return '—'
  }
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString()
}

async function run(
  action: () => Promise<unknown>,
  reload: () => Promise<void>,
  setError: (message: string | null) => void,
) {
  try {
    await action()
    await reload()
  } catch (caught: unknown) {
    setError(messageOf(caught))
  }
}
