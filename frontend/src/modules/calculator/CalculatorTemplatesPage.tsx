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
import { useNavigate } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import {
  activateCalculatorTemplate,
  createCalculatorTemplate,
  deactivateCalculatorTemplate,
  listCalculatorTemplates,
  updateCalculatorTemplate,
  type CalculatorTemplateSummary,
} from './calculatorTemplatesApi.ts'

export function CalculatorTemplatesPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const canManage = auth.hasPermission(PermissionCodes.calculatorManageTemplates)
  const [templates, setTemplates] = useState<CalculatorTemplateSummary[]>([])
  const [error, setError] = useState<string | null>(null)
  const [creating, setCreating] = useState(false)
  const [editing, setEditing] = useState<CalculatorTemplateSummary | null>(null)
  const [deactivating, setDeactivating] = useState<CalculatorTemplateSummary | null>(null)

  async function load() {
    try {
      setTemplates(await listCalculatorTemplates())
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const next = await listCalculatorTemplates()
        if (!cancelled) {
          setTemplates(next)
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
          Calculator templates
        </Typography>
        {canManage ? (
          <Button variant="contained" onClick={() => setCreating(true)}>
            Create template
          </Button>
        ) : null}
      </Stack>
      {error ? <Alert severity="error">{error}</Alert> : null}
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Name</TableCell>
            <TableCell>Latest published version</TableCell>
            <TableCell>Draft version</TableCell>
            <TableCell>Status</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {templates.map((template) => (
            <TableRow key={template.id}>
              <TableCell>{template.name}</TableCell>
              <TableCell>
                {template.latestPublishedVersion ? `v${template.latestPublishedVersion}` : 'None'}
              </TableCell>
              <TableCell>{template.draftVersion ? `v${template.draftVersion}` : 'None'}</TableCell>
              <TableCell>{template.isActive ? 'Active' : 'Inactive'}</TableCell>
              <TableCell>
                <Stack direction="row" spacing={1}>
                  <Button
                    size="small"
                    onClick={() => navigate(`/admin/calculator-templates/${template.id}`)}
                  >
                    Open
                  </Button>
                  {canManage ? (
                    <Button size="small" onClick={() => setEditing(template)}>
                      Edit
                    </Button>
                  ) : null}
                  {canManage && template.isActive ? (
                    <Button size="small" onClick={() => setDeactivating(template)}>
                      Deactivate
                    </Button>
                  ) : null}
                  {canManage && !template.isActive ? (
                    <Button
                      size="small"
                      onClick={() => {
                        void activateCalculatorTemplate(template.id).then(() => load())
                      }}
                    >
                      Activate
                    </Button>
                  ) : null}
                </Stack>
              </TableCell>
            </TableRow>
          ))}
          {templates.length === 0 ? (
            <TableRow>
              <TableCell colSpan={5}>No calculator templates yet.</TableCell>
            </TableRow>
          ) : null}
        </TableBody>
      </Table>
      {creating ? (
        <TemplateDialog
          title="New calculator template"
          onClose={() => setCreating(false)}
          onSave={async (name, description) => {
            const created = await createCalculatorTemplate({ name, description })
            navigate(`/admin/calculator-templates/${created.id}`)
          }}
        />
      ) : null}
      {editing ? (
        <TemplateDialog
          title="Edit calculator template"
          initialName={editing.name}
          initialDescription={editing.description ?? ''}
          onClose={() => setEditing(null)}
          onSave={async (name, description) => {
            await updateCalculatorTemplate(editing.id, { name, description })
            setEditing(null)
            await load()
          }}
        />
      ) : null}
      <Dialog open={deactivating !== null} onClose={() => setDeactivating(null)}>
        <DialogTitle>Deactivate calculator template?</DialogTitle>
        <DialogContent>
          <Typography>
            It will no longer be available for new Order Type assignments. Existing assignments stay
            in place.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeactivating(null)}>Cancel</Button>
          <Button
            variant="contained"
            onClick={() => {
              if (!deactivating) return
              void deactivateCalculatorTemplate(deactivating.id)
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

function TemplateDialog({
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
