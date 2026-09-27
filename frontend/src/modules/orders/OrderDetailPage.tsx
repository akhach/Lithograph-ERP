import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import Chip from '@mui/material/Chip'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { listOrderTypes, type OrderType } from './orderTypesApi.ts'
import {
  addChecklistItem,
  addFolderLink,
  changeOrderStatus,
  deleteChecklistItem,
  deleteFolderLink,
  getOrder,
  isPreviewUrl,
  money,
  orderPriorityLabels,
  orderStatusLabels,
  reorderChecklistItems,
  reorderFolderLinks,
  updateChecklistItem,
  updateFolderLink,
  updateOrder,
  type ChecklistItem,
  type FolderLink,
  type OrderDetail,
  type OrderPriority,
  type OrderStatus,
} from './ordersApi.ts'
import {
  listOpenProjectOptions,
  projectOptionLabel,
  type ProjectOption,
} from '../projects/projectsApi.ts'

export function OrderDetailPage() {
  const auth = useAuth()
  const { orderId = '' } = useParams()
  const canEdit = auth.hasPermission(PermissionCodes.ordersEdit)
  const canChangeStatus = auth.hasPermission(PermissionCodes.ordersChangeStatus)
  const canManageChecklist = auth.hasPermission(PermissionCodes.ordersManageChecklist)
  const canManageFolders = auth.hasPermission(PermissionCodes.ordersManageFolderLinks)
  const canSeeSelling = auth.hasPermission(PermissionCodes.ordersViewSellingPrice)
  const canSeeCost = auth.hasPermission(PermissionCodes.ordersViewCostPrice)
  const [order, setOrder] = useState<OrderDetail | null>(null)
  const [projects, setProjects] = useState<ProjectOption[]>([])
  const [orderTypes, setOrderTypes] = useState<OrderType[]>([])
  const [status, setStatus] = useState<OrderStatus>('draft')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)
  const [confirmComplete, setConfirmComplete] = useState(false)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [loaded, types, projectPage] = await Promise.all([
          getOrder(orderId),
          listOrderTypes(true),
          canEdit ? listOpenProjectOptions() : Promise.resolve({ items: [] as ProjectOption[] }),
        ])
        if (cancelled) return
        setOrder(loaded)
        setStatus(loaded.status)
        setOrderTypes(types)
        setProjects(projectPage.items)
        setError(null)
      } catch (caught: unknown) {
        if (!cancelled) {
          setOrder(null)
          setError(messageOf(caught))
        }
      }
    })()
    return () => {
      cancelled = true
    }
  }, [orderId, canEdit])

  async function save() {
    if (!order) return
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      setOrder(
        await updateOrder(order.id, {
          projectId: order.project.id,
          orderTypeId: order.orderType.id,
          name: order.name,
          description: order.description ?? '',
          priority: order.priority,
          deadline: order.deadline || undefined,
          previewImagePath: order.previewImagePath ?? '',
        }),
      )
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        previewImagePath: fieldMessage(caught, 'previewImagePath'),
      })
    } finally {
      setSaving(false)
    }
  }

  async function applyStatus(next = status, confirmed = false) {
    if (!order) return
    const incomplete = order.checklistProgress.total - order.checklistProgress.completed
    if (next === 'completed' && incomplete > 0 && !confirmed) {
      setConfirmComplete(true)
      return
    }
    setConfirmComplete(false)
    try {
      const updated = await changeOrderStatus(order.id, next)
      setOrder(updated)
      setStatus(updated.status)
      setError(null)
    } catch (caught: unknown) {
      setError(messageOf(caught))
    }
  }

  if (!order) {
    return (
      <Stack spacing={2}>
        <Button component={Link} to="/orders">
          Back to orders
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : <Typography>Loading order…</Typography>}
      </Stack>
    )
  }

  const typeChoices = orderTypes.some((type) => type.id === order.orderType.id)
    ? orderTypes
    : [
        {
          id: order.orderType.id,
          name: order.orderType.name,
          description: null,
          isActive: order.orderType.isActive,
          createdAt: '',
          updatedAt: null,
        },
        ...orderTypes,
      ]
  const projectChoices = projects.some((project) => project.id === order.project.id)
    ? projects
    : [
        {
          id: order.project.id,
          businessId: order.project.businessId,
          name: order.project.name,
          clientName: order.client.name,
          status: order.project.status as ProjectOption['status'],
        },
        ...projects,
      ]
  const selectedProject = projectChoices.find((project) => project.id === order.project.id)
  const projectChanged =
    selectedProject !== undefined && selectedProject.clientName !== order.client.name
  const deadlineLater =
    order.deadline !== null &&
    order.project.deadline !== null &&
    order.deadline > order.project.deadline
  const remaining = order.checklistProgress.total - order.checklistProgress.completed

  return (
    <Stack spacing={3}>
      <Button component={Link} to="/orders" sx={{ alignSelf: 'flex-start' }}>
        Back to orders
      </Button>
      <Stack direction="row" spacing={1} useFlexGap sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="h5" component="h2">
          {order.businessId}
        </Typography>
        <Chip label={orderStatusLabels[order.status]} />
        <Chip label={orderPriorityLabels[order.priority]} variant="outlined" />
      </Stack>
      <Typography variant="h6">{order.name}</Typography>
      <Typography>
        Project:{' '}
        <Link to={`/projects/${order.project.id}`}>
          {order.project.businessId} — {order.project.name}
        </Link>
      </Typography>
      <Typography>
        Client:{' '}
        <Link to={`/clients/${order.client.id}`}>
          {order.client.businessId} — {order.client.name}
        </Link>
      </Typography>
      {error ? <Alert severity="error">{error}</Alert> : null}
      {deadlineLater ? (
        <Alert severity="warning">
          This Order deadline is later than the Project deadline ({order.project.deadline}). Neither
          date is changed automatically.
        </Alert>
      ) : null}

      <Typography variant="h6" component="h3">
        General
      </Typography>
      <Stack spacing={2} sx={{ maxWidth: 720 }}>
        <TextField label="Order ID" value={order.businessId} disabled />
        {canEdit && order.status === 'draft' ? (
          <TextField
            select
            label="Project"
            value={order.project.id}
            onChange={(event) => {
              const next = projectChoices.find((project) => project.id === event.target.value)
              if (!next || !order) return
              setOrder({
                ...order,
                project: {
                  ...order.project,
                  id: next.id,
                  businessId: next.businessId,
                  name: next.name,
                },
              })
            }}
          >
            {projectChoices.map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {projectOptionLabel(project)}
              </MenuItem>
            ))}
          </TextField>
        ) : (
          <TextField
            label="Project"
            value={`${order.project.businessId} — ${order.project.name}`}
            disabled
          />
        )}
        {projectChanged ? (
          <Alert severity="info">
            Changing the Project also changes the Client and Project Team for this Order. New
            client: {selectedProject?.clientName}.
          </Alert>
        ) : null}
        <TextField
          select
          label="Order type"
          value={order.orderType.id}
          disabled={!canEdit}
          onChange={(event) => {
            const next = typeChoices.find((type) => type.id === event.target.value)
            if (!next) return
            setOrder({
              ...order,
              orderType: { id: next.id, name: next.name, isActive: next.isActive },
            })
          }}
        >
          {typeChoices.map((type) => (
            <MenuItem key={type.id} value={type.id}>
              {type.name}
              {type.isActive ? '' : ' — Inactive'}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Name"
          required
          value={order.name}
          disabled={!canEdit}
          error={Boolean(fieldErrors.name)}
          helperText={fieldErrors.name}
          onChange={(event) => setOrder({ ...order, name: event.target.value })}
        />
        <TextField
          label="Description"
          multiline
          minRows={2}
          value={order.description ?? ''}
          disabled={!canEdit}
          onChange={(event) => setOrder({ ...order, description: event.target.value })}
        />
        <TextField
          select
          label="Priority"
          value={order.priority}
          disabled={!canEdit}
          onChange={(event) =>
            setOrder({ ...order, priority: event.target.value as OrderPriority })
          }
        >
          {Object.entries(orderPriorityLabels).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          label="Deadline"
          type="date"
          slotProps={{ inputLabel: { shrink: true } }}
          value={order.deadline ?? ''}
          disabled={!canEdit}
          onChange={(event) => setOrder({ ...order, deadline: event.target.value || null })}
        />
        <TextField
          label="Preview image path"
          value={order.previewImagePath ?? ''}
          disabled={!canEdit}
          error={Boolean(fieldErrors.previewImagePath)}
          helperText={
            fieldErrors.previewImagePath ?? 'Optional reference. The image file is not uploaded.'
          }
          onChange={(event) => setOrder({ ...order, previewImagePath: event.target.value || null })}
        />
        {isPreviewUrl(order.previewImagePath) ? (
          <Box
            component="img"
            src={order.previewImagePath}
            alt=""
            sx={{ maxWidth: 320, maxHeight: 180, objectFit: 'contain' }}
          />
        ) : order.previewImagePath ? (
          <Typography variant="body2">{order.previewImagePath}</Typography>
        ) : null}
        {canSeeSelling && order.sellingPrice !== undefined ? (
          <TextField label="Selling price" value={money(order.sellingPrice)} disabled />
        ) : null}
        {canSeeCost && order.costPrice !== undefined ? (
          <TextField label="Cost price" value={money(order.costPrice)} disabled />
        ) : null}
        {canSeeSelling && canSeeCost && order.profit !== undefined ? (
          <TextField label="Profit" value={money(order.profit)} disabled />
        ) : null}
        {canEdit ? (
          <Button
            variant="contained"
            disabled={saving}
            onClick={() => void save()}
            sx={{ alignSelf: 'flex-start' }}
          >
            Save
          </Button>
        ) : null}
      </Stack>

      {canChangeStatus ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <TextField
            select
            label="Status"
            size="small"
            value={status}
            onChange={(event) => setStatus(event.target.value as OrderStatus)}
            sx={{ minWidth: 180 }}
          >
            {Object.entries(orderStatusLabels).map(([value, label]) => (
              <MenuItem key={value} value={value}>
                {label}
              </MenuItem>
            ))}
          </TextField>
          <Button variant="outlined" onClick={() => void applyStatus()}>
            Update status
          </Button>
        </Stack>
      ) : null}

      <Typography variant="h6" component="h3">
        Project team
      </Typography>
      <Stack spacing={0.5}>
        <Typography>Owner: {personLabel(order.team.owner)}</Typography>
        <Typography>Assignee: {personLabel(order.team.assignee)}</Typography>
        <Typography>Participants: {peopleLabel(order.team.participants)}</Typography>
        <Typography>Observers: {peopleLabel(order.team.observers)}</Typography>
        <Button
          component={Link}
          to={`/projects/${order.project.id}`}
          sx={{ alignSelf: 'flex-start' }}
        >
          Open project
        </Button>
      </Stack>

      <Typography variant="h6" component="h3">
        Calculator
      </Typography>
      <Alert severity="info">Calculator module not configured.</Alert>

      <ChecklistSection
        orderId={order.id}
        items={order.checklistItems}
        progress={order.checklistProgress}
        canManage={canManageChecklist}
        onChange={(checklistItems, checklistProgress) =>
          setOrder({ ...order, checklistItems, checklistProgress })
        }
        onError={setError}
      />
      <FolderSection
        orderId={order.id}
        links={order.folderLinks}
        canManage={canManageFolders}
        onChange={(folderLinks) => setOrder({ ...order, folderLinks })}
        onError={setError}
      />

      <Dialog open={confirmComplete} onClose={() => setConfirmComplete(false)}>
        <DialogTitle>Complete order?</DialogTitle>
        <DialogContent>
          <Typography>
            This Order has {remaining} incomplete Checklist {remaining === 1 ? 'Item' : 'Items'}.
            Complete the Order anyway?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmComplete(false)}>Cancel</Button>
          <Button variant="contained" onClick={() => void applyStatus('completed', true)}>
            Complete order
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

function personLabel(person: OrderDetail['team']['owner']): string {
  if (!person) return 'Not assigned'
  return person.isActive ? person.fullName : `${person.fullName} — Inactive`
}

function peopleLabel(people: OrderDetail['team']['participants']): string {
  if (people.length === 0) return 'None'
  return people.map((person) => personLabel(person)).join(', ')
}

function ChecklistSection({
  orderId,
  items,
  progress,
  canManage,
  onChange,
  onError,
}: {
  orderId: string
  items: ChecklistItem[]
  progress: OrderDetail['checklistProgress']
  canManage: boolean
  onChange: (items: ChecklistItem[], progress: OrderDetail['checklistProgress']) => void
  onError: (message: string) => void
}) {
  const [text, setText] = useState('')

  function publish(next: ChecklistItem[]) {
    onChange(next, {
      completed: next.filter((item) => item.isCompleted).length,
      total: next.length,
    })
  }

  async function add() {
    try {
      const created = await addChecklistItem(orderId, text)
      publish([...items, created])
      setText('')
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function toggle(item: ChecklistItem) {
    try {
      const updated = await updateChecklistItem(orderId, item.id, {
        isCompleted: !item.isCompleted,
      })
      publish(items.map((current) => (current.id === item.id ? updated : current)))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function rename(item: ChecklistItem, nextText: string) {
    if (nextText.trim() === item.text) return
    try {
      const updated = await updateChecklistItem(orderId, item.id, { text: nextText })
      publish(items.map((current) => (current.id === item.id ? updated : current)))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function remove(itemId: string) {
    try {
      await deleteChecklistItem(orderId, itemId)
      publish(items.filter((item) => item.id !== itemId))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function move(index: number, direction: -1 | 1) {
    const target = index + direction
    if (target < 0 || target >= items.length) return
    const ids = items.map((item) => item.id)
    const [moved] = ids.splice(index, 1)
    ids.splice(target, 0, moved)
    try {
      publish(await reorderChecklistItems(orderId, ids))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  return (
    <Stack spacing={1}>
      <Typography variant="h6" component="h3">
        Checklist
      </Typography>
      <Typography variant="body2">
        {progress.completed} / {progress.total} completed
      </Typography>
      {items.length === 0 ? <Typography>No Checklist Items yet.</Typography> : null}
      {items.map((item, index) => (
        <Stack key={item.id} direction="row" spacing={1} sx={{ alignItems: 'center' }}>
          <Checkbox
            checked={item.isCompleted}
            disabled={!canManage}
            onChange={() => void toggle(item)}
            slotProps={{ input: { 'aria-label': `Complete ${item.text}` } }}
          />
          <TextField
            size="small"
            fullWidth
            defaultValue={item.text}
            disabled={!canManage}
            key={`${item.id}-${item.text}`}
            onBlur={(event) => void rename(item, event.target.value)}
          />
          {canManage ? (
            <>
              <Button size="small" disabled={index === 0} onClick={() => void move(index, -1)}>
                Up
              </Button>
              <Button
                size="small"
                disabled={index === items.length - 1}
                onClick={() => void move(index, 1)}
              >
                Down
              </Button>
              <Button size="small" color="error" onClick={() => void remove(item.id)}>
                Delete
              </Button>
            </>
          ) : null}
        </Stack>
      ))}
      {canManage ? (
        <Stack direction="row" spacing={1}>
          <TextField
            size="small"
            label="New item"
            value={text}
            onChange={(event) => setText(event.target.value)}
            sx={{ flexGrow: 1 }}
          />
          <Button variant="outlined" disabled={!text.trim()} onClick={() => void add()}>
            Add item
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )
}

function FolderSection({
  orderId,
  links,
  canManage,
  onChange,
  onError,
}: {
  orderId: string
  links: FolderLink[]
  canManage: boolean
  onChange: (links: FolderLink[]) => void
  onError: (message: string) => void
}) {
  const [name, setName] = useState('')
  const [path, setPath] = useState('')

  async function add() {
    try {
      const created = await addFolderLink(orderId, { name: name || undefined, path })
      onChange([...links, created])
      setName('')
      setPath('')
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function save(link: FolderLink, nextName: string, nextPath: string) {
    if (nextPath.trim() === link.path && (nextName.trim() || null) === link.name) return
    try {
      const updated = await updateFolderLink(orderId, link.id, { name: nextName, path: nextPath })
      onChange(links.map((current) => (current.id === link.id ? updated : current)))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function remove(linkId: string) {
    try {
      await deleteFolderLink(orderId, linkId)
      onChange(links.filter((link) => link.id !== linkId))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  async function move(index: number, direction: -1 | 1) {
    const target = index + direction
    if (target < 0 || target >= links.length) return
    const ids = links.map((link) => link.id)
    const [moved] = ids.splice(index, 1)
    ids.splice(target, 0, moved)
    try {
      onChange(await reorderFolderLinks(orderId, ids))
    } catch (caught: unknown) {
      onError(messageOf(caught))
    }
  }

  return (
    <Stack spacing={1}>
      <Typography variant="h6" component="h3">
        Folder links
      </Typography>
      {links.length === 0 ? <Typography>No Folder Links yet.</Typography> : null}
      {links.map((link, index) => (
        <Stack
          key={link.id}
          direction="row"
          spacing={1}
          useFlexGap
          sx={{ alignItems: 'center', flexWrap: 'wrap' }}
        >
          <TextField
            size="small"
            label="Name"
            defaultValue={link.name ?? ''}
            disabled={!canManage}
            onBlur={(event) => void save(link, event.target.value, link.path)}
          />
          <TextField
            size="small"
            label="Path"
            defaultValue={link.path}
            disabled={!canManage}
            sx={{ minWidth: 280, flexGrow: 1 }}
            onBlur={(event) => void save(link, link.name ?? '', event.target.value)}
          />
          <Button size="small" onClick={() => void navigator.clipboard.writeText(link.path)}>
            Copy path
          </Button>
          {canManage ? (
            <>
              <Button size="small" disabled={index === 0} onClick={() => void move(index, -1)}>
                Up
              </Button>
              <Button
                size="small"
                disabled={index === links.length - 1}
                onClick={() => void move(index, 1)}
              >
                Down
              </Button>
              <Button size="small" color="error" onClick={() => void remove(link.id)}>
                Delete
              </Button>
            </>
          ) : null}
        </Stack>
      ))}
      {canManage ? (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <TextField
            size="small"
            label="Name"
            value={name}
            onChange={(event) => setName(event.target.value)}
          />
          <TextField
            size="small"
            label="Path"
            required
            value={path}
            onChange={(event) => setPath(event.target.value)}
            sx={{ minWidth: 280 }}
          />
          <Button variant="outlined" disabled={!path.trim()} onClick={() => void add()}>
            Add folder link
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )
}
