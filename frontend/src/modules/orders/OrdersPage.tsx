import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { fieldMessage, messageOf } from '../auth/apiMessages.ts'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { listClients, type ClientListItem } from '../clients/clientsApi.ts'
import { listActiveEmployeeOptions, type EmployeeOption } from '../employees/employeesApi.ts'
import {
  listOpenProjectOptions,
  listProjects,
  projectOptionLabel,
  type ProjectListItem,
  type ProjectOption,
} from '../projects/projectsApi.ts'
import { listOrderTypes, type OrderType } from './orderTypesApi.ts'
import {
  createOrder,
  isPreviewUrl,
  listOrders,
  money,
  orderPriorityLabels,
  orderStatusLabels,
  type OrderListItem,
  type OrderPriority,
  type OrderStatus,
} from './ordersApi.ts'

type StatusFilter = 'open' | 'all' | OrderStatus

type Filters = {
  search: string
  status: StatusFilter
  priority: '' | OrderPriority
  clientId: string
  projectId: string
  orderTypeId: string
  ownerEmployeeId: string
  assigneeEmployeeId: string
  deadlineFrom: string
  deadlineTo: string
  sort: 'created_at' | 'deadline' | 'priority' | 'business_id' | 'name' | 'status'
  page: number
}

export function OrdersPage() {
  const auth = useAuth()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const canCreate = auth.hasPermission(PermissionCodes.ordersCreate)
  const canSeeSelling = auth.hasPermission(PermissionCodes.ordersViewSellingPrice)
  const canSeeCost = auth.hasPermission(PermissionCodes.ordersViewCostPrice)
  const canSeeProfit = canSeeSelling && canSeeCost
  const [filters, setFilters] = useState<Filters>({
    search: '',
    status: 'open',
    priority: '',
    clientId: '',
    projectId: '',
    orderTypeId: '',
    ownerEmployeeId: '',
    assigneeEmployeeId: '',
    deadlineFrom: '',
    deadlineTo: '',
    sort: 'created_at',
    page: 1,
  })
  const [draftSearch, setDraftSearch] = useState('')
  const [orders, setOrders] = useState<OrderListItem[]>([])
  const [clients, setClients] = useState<ClientListItem[]>([])
  const [projects, setProjects] = useState<ProjectListItem[]>([])
  const [openProjects, setOpenProjects] = useState<ProjectOption[]>([])
  const [employees, setEmployees] = useState<EmployeeOption[]>([])
  const [orderTypes, setOrderTypes] = useState<OrderType[]>([])
  const [activeTypes, setActiveTypes] = useState<OrderType[]>([])
  const [totalPages, setTotalPages] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [gallery, setGallery] = useState(false)
  const [creating, setCreating] = useState(searchParams.has('projectId'))
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [typeList, activeList, projectOptions] = await Promise.all([
          listOrderTypes(),
          listOrderTypes(true),
          canCreate ? listOpenProjectOptions() : Promise.resolve({ items: [] as ProjectOption[] }),
        ])
        if (cancelled) return
        setOrderTypes(typeList)
        setActiveTypes(activeList)
        setOpenProjects(projectOptions.items)
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [canCreate])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const [clientPage, projectPage, employeePage] = await Promise.all([
          auth.hasPermission(PermissionCodes.clientsView)
            ? listClients({ pageSize: 200, sort: 'name' })
            : Promise.resolve({ items: [] as ClientListItem[] }),
          auth.hasPermission(PermissionCodes.projectsView)
            ? listProjects({ pageSize: 200, sort: 'name', direction: 'asc' })
            : Promise.resolve({ items: [] as ProjectListItem[] }),
          auth.hasPermission(PermissionCodes.employeesView)
            ? listActiveEmployeeOptions()
            : Promise.resolve({ items: [] as EmployeeOption[] }),
        ])
        if (cancelled) return
        setClients(clientPage.items)
        setProjects(projectPage.items)
        setEmployees(employeePage.items)
      } catch (caught: unknown) {
        if (!cancelled) setError(messageOf(caught))
      }
    })()
    return () => {
      cancelled = true
    }
  }, [auth])

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const page = await listOrders({
          search: filters.search || undefined,
          status: filters.status === 'all' ? undefined : filters.status,
          priority: filters.priority || undefined,
          clientId: filters.clientId || undefined,
          projectId: filters.projectId || undefined,
          orderTypeId: filters.orderTypeId || undefined,
          ownerEmployeeId: filters.ownerEmployeeId || undefined,
          assigneeEmployeeId: filters.assigneeEmployeeId || undefined,
          deadlineFrom: filters.deadlineFrom || undefined,
          deadlineTo: filters.deadlineTo || undefined,
          page: filters.page,
          sort: filters.sort,
          direction: filters.sort === 'created_at' ? 'desc' : 'asc',
        })
        if (cancelled) return
        setOrders(page.items)
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

  function closeCreate() {
    setCreating(false)
    if (searchParams.has('projectId')) {
      searchParams.delete('projectId')
      setSearchParams(searchParams, { replace: true })
    }
  }

  const columns = 9 + (canSeeSelling ? 1 : 0) + (canSeeCost ? 1 : 0) + (canSeeProfit ? 1 : 0)

  return (
    <Stack spacing={2}>
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Typography variant="h5" component="h2">
          Orders
        </Typography>
        <Stack direction="row" spacing={1}>
          <Button
            variant={gallery ? 'contained' : 'outlined'}
            onClick={() => setGallery((current) => !current)}
          >
            {gallery ? 'List view' : 'Gallery view'}
          </Button>
          {canCreate ? (
            <Button variant="contained" onClick={() => setCreating(true)}>
              Create order
            </Button>
          ) : null}
        </Stack>
      </Stack>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField
          label="Search orders"
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
          {Object.entries(orderStatusLabels).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Priority"
          size="small"
          value={filters.priority}
          onChange={(event) =>
            setFilters((current) => ({
              ...current,
              priority: event.target.value as Filters['priority'],
              page: 1,
            }))
          }
          sx={{ minWidth: 140 }}
        >
          <MenuItem value="">Any priority</MenuItem>
          {Object.entries(orderPriorityLabels).map(([value, label]) => (
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
          label="Project"
          size="small"
          value={filters.projectId}
          onChange={(event) =>
            setFilters((current) => ({ ...current, projectId: event.target.value, page: 1 }))
          }
          sx={{ minWidth: 180 }}
        >
          <MenuItem value="">All projects</MenuItem>
          {projects.map((project) => (
            <MenuItem key={project.id} value={project.id}>
              {project.businessId} — {project.name}
            </MenuItem>
          ))}
        </TextField>
        <TextField
          select
          label="Order type"
          size="small"
          value={filters.orderTypeId}
          onChange={(event) =>
            setFilters((current) => ({ ...current, orderTypeId: event.target.value, page: 1 }))
          }
          sx={{ minWidth: 160 }}
        >
          <MenuItem value="">All types</MenuItem>
          {orderTypes.map((type) => (
            <MenuItem key={type.id} value={type.id}>
              {type.name}
              {type.isActive ? '' : ' — Inactive'}
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
          select
          label="Sort"
          size="small"
          value={filters.sort}
          onChange={(event) =>
            setFilters((current) => ({
              ...current,
              sort: event.target.value as Filters['sort'],
              page: 1,
            }))
          }
          sx={{ minWidth: 150 }}
        >
          <MenuItem value="created_at">Created</MenuItem>
          <MenuItem value="deadline">Deadline</MenuItem>
          <MenuItem value="priority">Priority</MenuItem>
          <MenuItem value="business_id">Order ID</MenuItem>
          <MenuItem value="name">Name</MenuItem>
          <MenuItem value="status">Status</MenuItem>
        </TextField>
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
      {gallery ? (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))',
            gap: 2,
          }}
        >
          {orders.map((order) => (
            <Paper
              key={order.id}
              variant="outlined"
              sx={{ p: 1.5, cursor: 'pointer' }}
              onClick={() => navigate(`/orders/${order.id}`)}
            >
              <Stack spacing={1}>
                {isPreviewUrl(order.previewImagePath) ? (
                  <Box
                    component="img"
                    src={order.previewImagePath}
                    alt=""
                    sx={{ width: '100%', height: 140, objectFit: 'cover', bgcolor: 'action.hover' }}
                  />
                ) : (
                  <Box
                    sx={{
                      height: 140,
                      display: 'grid',
                      placeItems: 'center',
                      bgcolor: 'action.hover',
                      px: 1,
                    }}
                  >
                    <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center' }}>
                      {order.previewImagePath ?? 'No preview'}
                    </Typography>
                  </Box>
                )}
                <Typography variant="body2">{order.businessId}</Typography>
                <Typography variant="subtitle2">{order.name}</Typography>
                <Typography variant="body2">{order.client.name}</Typography>
                <Typography variant="body2">{order.project.name}</Typography>
                <Typography variant="body2">{order.orderType.name}</Typography>
              </Stack>
            </Paper>
          ))}
          {orders.length === 0 ? <Typography>No orders match this search.</Typography> : null}
        </Box>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Order ID</TableCell>
              <TableCell>Order</TableCell>
              <TableCell>Client</TableCell>
              <TableCell>Project</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Priority</TableCell>
              <TableCell>Deadline</TableCell>
              {canSeeSelling ? <TableCell>Selling price</TableCell> : null}
              {canSeeCost ? <TableCell>Cost price</TableCell> : null}
              {canSeeProfit ? <TableCell>Profit</TableCell> : null}
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {orders.map((order) => (
              <TableRow
                key={order.id}
                hover
                sx={{ cursor: 'pointer' }}
                onDoubleClick={() => navigate(`/orders/${order.id}`)}
              >
                <TableCell>{order.businessId}</TableCell>
                <TableCell>{order.name}</TableCell>
                <TableCell>{order.client.name}</TableCell>
                <TableCell>{order.project.name}</TableCell>
                <TableCell>{order.orderType.name}</TableCell>
                <TableCell>
                  <Chip size="small" label={orderStatusLabels[order.status]} />
                </TableCell>
                <TableCell>{orderPriorityLabels[order.priority]}</TableCell>
                <TableCell>{order.deadline ?? ''}</TableCell>
                {canSeeSelling ? (
                  <TableCell>
                    {order.sellingPrice === undefined ? '' : money(order.sellingPrice)}
                  </TableCell>
                ) : null}
                {canSeeCost ? (
                  <TableCell>
                    {order.costPrice === undefined ? '' : money(order.costPrice)}
                  </TableCell>
                ) : null}
                {canSeeProfit ? (
                  <TableCell>{order.profit === undefined ? '' : money(order.profit)}</TableCell>
                ) : null}
                <TableCell>
                  <Button size="small" onClick={() => navigate(`/orders/${order.id}`)}>
                    Open
                  </Button>
                </TableCell>
              </TableRow>
            ))}
            {orders.length === 0 ? (
              <TableRow>
                <TableCell colSpan={columns}>No orders match this search.</TableCell>
              </TableRow>
            ) : null}
          </TableBody>
        </Table>
      )}
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
        <OrderCreateDialog
          projects={openProjects}
          orderTypes={activeTypes}
          initialProjectId={searchParams.get('projectId') ?? ''}
          onClose={closeCreate}
          onCreated={(orderId) => {
            closeCreate()
            setReloadKey((current) => current + 1)
            navigate(`/orders/${orderId}`)
          }}
        />
      ) : null}
    </Stack>
  )
}

function OrderCreateDialog({
  projects,
  orderTypes,
  initialProjectId,
  onClose,
  onCreated,
}: {
  projects: ProjectOption[]
  orderTypes: OrderType[]
  initialProjectId: string
  onClose: () => void
  onCreated: (orderId: string) => void
}) {
  const initial = projects.some((project) => project.id === initialProjectId)
    ? initialProjectId
    : (projects[0]?.id ?? '')
  const [projectId, setProjectId] = useState(initial)
  const [orderTypeId, setOrderTypeId] = useState(orderTypes[0]?.id ?? '')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [priority, setPriority] = useState<OrderPriority>('normal')
  const [deadline, setDeadline] = useState('')
  const [previewImagePath, setPreviewImagePath] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string | undefined>>({})
  const [saving, setSaving] = useState(false)
  const selectedProject = projects.find((project) => project.id === projectId)

  async function submit() {
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const created = await createOrder({
        projectId,
        orderTypeId,
        name,
        description: description || undefined,
        priority,
        deadline: deadline || undefined,
        previewImagePath: previewImagePath || undefined,
      })
      onCreated(created.id)
    } catch (caught: unknown) {
      setError(messageOf(caught))
      setFieldErrors({
        name: fieldMessage(caught, 'name'),
        previewImagePath: fieldMessage(caught, 'previewImagePath'),
      })
      setSaving(false)
    }
  }

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>New order</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error ? <Alert severity="error">{error}</Alert> : null}
          <TextField label="Order ID" value="Generated automatically" disabled />
          <TextField
            select
            label="Project"
            required
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
          >
            {projects.map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {projectOptionLabel(project)}
              </MenuItem>
            ))}
          </TextField>
          {selectedProject ? (
            <Typography variant="body2" color="text.secondary">
              Client: {selectedProject.clientName}
            </Typography>
          ) : null}
          <TextField
            select
            label="Order type"
            required
            value={orderTypeId}
            onChange={(event) => setOrderTypeId(event.target.value)}
          >
            {orderTypes.map((type) => (
              <MenuItem key={type.id} value={type.id}>
                {type.name}
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
          <TextField label="Status" value="Draft" disabled />
          <TextField
            select
            label="Priority"
            value={priority}
            onChange={(event) => setPriority(event.target.value as OrderPriority)}
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
            value={deadline}
            onChange={(event) => setDeadline(event.target.value)}
          />
          <TextField
            label="Preview image path"
            value={previewImagePath}
            error={Boolean(fieldErrors.previewImagePath)}
            helperText={
              fieldErrors.previewImagePath ?? 'Optional reference. The image file is not uploaded.'
            }
            onChange={(event) => setPreviewImagePath(event.target.value)}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button
          variant="contained"
          disabled={saving || !projectId || !orderTypeId}
          onClick={() => void submit()}
        >
          Create
        </Button>
      </DialogActions>
    </Dialog>
  )
}
