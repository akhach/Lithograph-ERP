import Alert from '@mui/material/Alert'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import FormControlLabel from '@mui/material/FormControlLabel'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useMemo, useState } from 'react'
import { useAuth } from '../auth/authContext.ts'
import {
  createRole,
  listPermissions,
  listRoles,
  setRolePermissions,
  updateRole,
} from '../auth/authApi.ts'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { PermissionCodes, type Permission, type Role } from '../auth/authTypes.ts'

export function RolesPage() {
  const auth = useAuth()
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<Permission[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const canCreate = auth.hasPermission(PermissionCodes.rolesCreate)
  const canEdit = auth.hasPermission(PermissionCodes.rolesEdit)
  const canManagePermissions = auth.hasPermission(PermissionCodes.rolesManagePermissions)

  async function load(selectId?: string) {
    setError(null)
    try {
      const [nextRoles, nextPermissions] = await Promise.all([listRoles(), listPermissions()])
      setRoles(nextRoles)
      setPermissions(nextPermissions)
      setSelectedId((current) => selectId ?? current ?? nextRoles[0]?.id ?? null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [nextRoles, nextPermissions] = await Promise.all([listRoles(), listPermissions()])
        if (cancelled) {
          return
        }
        setRoles(nextRoles)
        setPermissions(nextPermissions)
        setSelectedId((current) => current ?? nextRoles[0]?.id ?? null)
      } catch (caught: unknown) {
        if (!cancelled) {
          setError(messageOf(caught))
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  const selected = roles.find((role) => role.id === selectedId) ?? null

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Roles
        </Typography>
        {canCreate ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create role
          </Button>
        ) : null}
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'flex-start' }}>
        <Paper variant="outlined" sx={{ minWidth: 220 }}>
          <Stack>
            {roles.map((role) => (
              <Button
                key={role.id}
                onClick={() => setSelectedId(role.id)}
                variant={role.id === selectedId ? 'contained' : 'text'}
                sx={{ justifyContent: 'flex-start', borderRadius: 0 }}
              >
                {role.name}
              </Button>
            ))}
          </Stack>
        </Paper>
        {selected ? (
          <RoleEditor
            key={`${selected.id}:${selected.name}:${selected.description ?? ''}:${selected.permissions.map((permission) => permission.id).join(',')}`}
            role={selected}
            permissions={permissions}
            canEdit={canEdit}
            canManagePermissions={canManagePermissions}
            onSaved={async () => {
              await load(selected.id)
              await auth.reloadUser()
            }}
          />
        ) : null}
      </Stack>
      <CreateRoleDialog
        open={creating}
        onClose={() => setCreating(false)}
        onCreated={(role) => {
          setCreating(false)
          void load(role.id)
        }}
      />
    </Stack>
  )
}

function RoleEditor({
  role,
  permissions,
  canEdit,
  canManagePermissions,
  onSaved,
}: {
  role: Role
  permissions: Permission[]
  canEdit: boolean
  canManagePermissions: boolean
  onSaved: () => Promise<void>
}) {
  const [name, setName] = useState(role.name)
  const [description, setDescription] = useState(role.description ?? '')
  const [selectedIds, setSelectedIds] = useState<string[]>(() =>
    role.permissions.map((item) => item.id),
  )
  const [error, setError] = useState<unknown>(null)
  const [saving, setSaving] = useState(false)

  const grouped = useMemo(() => {
    const groups = new Map<string, Permission[]>()
    for (const permission of permissions) {
      const list = groups.get(permission.module) ?? []
      list.push(permission)
      groups.set(permission.module, list)
    }
    return [...groups.entries()]
  }, [permissions])

  async function saveDetails(event: React.FormEvent) {
    event.preventDefault()
    setSaving(true)
    setError(null)
    try {
      await updateRole(role.id, name, description)
      await onSaved()
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSaving(false)
    }
  }

  async function savePermissions() {
    setSaving(true)
    setError(null)
    try {
      await setRolePermissions(role.id, selectedIds)
      await onSaved()
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSaving(false)
    }
  }

  return (
    <Paper variant="outlined" sx={{ p: 2, flex: 1, width: '100%' }}>
      <Stack spacing={2}>
        <form onSubmit={(event) => void saveDetails(event)}>
          <Stack spacing={2}>
            <TextField
              label="Name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              disabled={!canEdit || role.isSystem}
              helperText={
                role.isSystem ? 'The Director role name is protected.' : fieldMessage(error, 'name')
              }
              error={Boolean(fieldMessage(error, 'name'))}
              required
            />
            <TextField
              label="Description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              disabled={!canEdit}
              multiline
              minRows={2}
            />
            {canEdit ? (
              <Button
                type="submit"
                variant="contained"
                disabled={saving}
                sx={{ alignSelf: 'flex-start' }}
              >
                Save role
              </Button>
            ) : null}
          </Stack>
        </form>
        <Typography variant="h6" component="h3">
          Permissions
        </Typography>
        {role.isSystem ? (
          <Alert severity="info">Director receives every current and future permission.</Alert>
        ) : null}
        {grouped.map(([module, items]) => (
          <Stack key={module} spacing={0}>
            <Typography variant="subtitle2">{module}</Typography>
            {items.map((permission) => (
              <FormControlLabel
                key={permission.id}
                control={
                  <Checkbox
                    checked={role.isSystem || selectedIds.includes(permission.id)}
                    disabled={role.isSystem || !canManagePermissions}
                    onChange={(event) =>
                      setSelectedIds((current) =>
                        event.target.checked
                          ? [...current, permission.id]
                          : current.filter((id) => id !== permission.id),
                      )
                    }
                  />
                }
                label={`${permission.name} (${permission.code})`}
              />
            ))}
          </Stack>
        ))}
        {canManagePermissions && !role.isSystem ? (
          <Button
            variant="contained"
            disabled={saving}
            onClick={() => void savePermissions()}
            sx={{ alignSelf: 'flex-start' }}
          >
            Save permissions
          </Button>
        ) : null}
        {error && !fieldMessage(error, 'name') ? (
          <Alert severity="error">{messageOf(error)}</Alert>
        ) : null}
      </Stack>
    </Paper>
  )
}

function CreateRoleDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean
  onClose: () => void
  onCreated: (role: Role) => void
}) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  function close() {
    setName('')
    setDescription('')
    setError(null)
    onClose()
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const role = await createRole(name, description)
      setName('')
      setDescription('')
      onCreated(role)
    } catch (caught: unknown) {
      setError(caught)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={close} fullWidth maxWidth="xs">
      <form onSubmit={(event) => void onSubmit(event)}>
        <DialogTitle>Create role</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 1 }}>
            <TextField
              label="Name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              error={Boolean(fieldMessage(error, 'name'))}
              helperText={fieldMessage(error, 'name')}
              required
            />
            <TextField
              label="Description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              multiline
              minRows={2}
            />
            {error && !fieldMessage(error, 'name') ? (
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
