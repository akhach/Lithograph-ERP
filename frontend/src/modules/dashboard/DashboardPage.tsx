import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import Stack from '@mui/material/Stack'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import Typography from '@mui/material/Typography'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { messageOf } from '../auth/apiMessages.ts'
import {
  orderPriorityLabels,
  orderStatusLabels,
  type OrderPriority,
  type OrderStatus,
} from '../orders/ordersApi.ts'
import { projectStatusLabels, type ProjectStatus } from '../projects/projectsApi.ts'
import {
  getDashboard,
  type DashboardOrder,
  type DashboardProject,
  type DashboardResponse,
} from './dashboardApi.ts'

export function DashboardPage() {
  const [data, setData] = useState<DashboardResponse | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    async function load() {
      await Promise.resolve()
      if (!active) {
        return
      }
      setLoading(true)
      setError(null)
      try {
        const result = await getDashboard()
        if (active) {
          setData(result)
        }
      } catch (caught) {
        if (active) {
          setError(messageOf(caught))
        }
      } finally {
        if (active) {
          setLoading(false)
        }
      }
    }
    void load()
    return () => {
      active = false
    }
  }, [reloadKey])

  if (loading && !data) {
    return (
      <Stack spacing={2}>
        <Typography variant="h5" component="h2">
          Dashboard
        </Typography>
        <Skeleton variant="rounded" height={88} />
        <Skeleton variant="rounded" height={180} />
        <Skeleton variant="rounded" height={180} />
      </Stack>
    )
  }

  if (error && !data) {
    return (
      <Stack spacing={2}>
        <Typography variant="h5" component="h2">
          Dashboard
        </Typography>
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={() => setReloadKey((value) => value + 1)}>
              Retry
            </Button>
          }
        >
          Dashboard could not be loaded.
        </Alert>
      </Stack>
    )
  }

  if (!data?.summary && !data?.recentProjects) {
    return (
      <Stack spacing={2}>
        <Typography variant="h5" component="h2">
          Dashboard
        </Typography>
        <Typography color="text.secondary">
          There are no Orders or Projects sections available for this account.
        </Typography>
      </Stack>
    )
  }

  return (
    <Stack spacing={3}>
      <Box>
        <Typography variant="h5" component="h2">
          Dashboard
        </Typography>
        <Typography variant="body2" color="text.secondary">
          What needs attention now. Open an Order or Project to do the work.
        </Typography>
      </Box>
      {error ? (
        <Alert
          severity="error"
          action={
            <Button color="inherit" onClick={() => setReloadKey((value) => value + 1)}>
              Retry
            </Button>
          }
        >
          Dashboard could not be loaded.
        </Alert>
      ) : null}
      {data.summary ? (
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          <SummaryCard
            label="Active Orders"
            value={data.summary.activeOrders}
            to="/orders?status=active"
          />
          <SummaryCard
            label="Urgent Orders"
            value={data.summary.urgentOrders}
            to="/orders?priority=urgent"
          />
          <SummaryCard label="Due Soon" value={data.summary.dueSoonOrders} to="/orders" />
          <SummaryCard label="Overdue" value={data.summary.overdueOrders} to="/orders" emphasis />
        </Stack>
      ) : null}
      {data.overdueOrders ? (
        <OrderSection
          title="Overdue"
          empty="No overdue Orders."
          orders={data.overdueOrders}
          emphasis
          columns={['client', 'status', 'priority', 'deadline']}
        />
      ) : null}
      {data.urgentOrders ? (
        <OrderSection
          title="Urgent"
          empty="No urgent Orders."
          orders={data.urgentOrders}
          emphasis
          columns={['client', 'project', 'status', 'deadline']}
        />
      ) : null}
      {data.dueSoonOrders ? (
        <OrderSection
          title="Due soon"
          empty="No Orders are due in the next 7 days."
          orders={data.dueSoonOrders}
          columns={['client', 'deadline', 'priority', 'status']}
        />
      ) : null}
      {data.activeOrders ? (
        <OrderSection
          title="Active Orders"
          empty="No active Orders."
          orders={data.activeOrders}
          viewAll="/orders?status=active"
          columns={['client', 'project', 'orderType', 'priority', 'deadline']}
        />
      ) : null}
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: 'stretch' }}>
        {data.recentOrders ? (
          <Box sx={{ flex: 1 }}>
            <OrderSection
              title="Recent Orders"
              empty="No recent Orders."
              orders={data.recentOrders}
              columns={['client', 'project', 'status', 'created']}
            />
          </Box>
        ) : null}
        {data.recentProjects ? (
          <Box sx={{ flex: 1 }}>
            <ProjectSection projects={data.recentProjects} />
          </Box>
        ) : null}
      </Stack>
    </Stack>
  )
}

function SummaryCard({
  label,
  value,
  to,
  emphasis = false,
}: {
  label: string
  value: number
  to: string
  emphasis?: boolean
}) {
  return (
    <Paper
      variant="outlined"
      sx={{ minWidth: 160, borderColor: emphasis && value > 0 ? 'warning.main' : undefined }}
    >
      <Box
        component={Link}
        to={to}
        sx={{ display: 'block', px: 2, py: 1.5, textDecoration: 'none', color: 'inherit' }}
      >
        <Typography variant="caption" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="h5">{value}</Typography>
      </Box>
    </Paper>
  )
}

function OrderSection({
  title,
  empty,
  orders,
  columns,
  emphasis = false,
  viewAll,
}: {
  title: string
  empty: string
  orders: DashboardOrder[]
  columns: Array<
    'client' | 'project' | 'orderType' | 'status' | 'priority' | 'deadline' | 'created'
  >
  emphasis?: boolean
  viewAll?: string
}) {
  return (
    <Paper
      variant="outlined"
      sx={{ p: 2, borderColor: emphasis && orders.length > 0 ? 'warning.main' : undefined }}
    >
      <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
        <Typography variant="h6" component="h3">
          {title}
        </Typography>
        {viewAll ? (
          <Button component={Link} to={viewAll} size="small">
            View all
          </Button>
        ) : null}
      </Stack>
      {orders.length === 0 ? (
        <Typography color="text.secondary">{empty}</Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Order</TableCell>
              {columns.includes('client') ? <TableCell>Client</TableCell> : null}
              {columns.includes('project') ? <TableCell>Project</TableCell> : null}
              {columns.includes('orderType') ? <TableCell>Order type</TableCell> : null}
              {columns.includes('status') ? <TableCell>Status</TableCell> : null}
              {columns.includes('priority') ? <TableCell>Priority</TableCell> : null}
              {columns.includes('deadline') ? <TableCell>Deadline</TableCell> : null}
              {columns.includes('created') ? <TableCell>Created</TableCell> : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {orders.map((order) => (
              <TableRow key={order.id} hover>
                <TableCell>
                  <Link to={`/orders/${order.id}`}>{order.businessId}</Link>
                  <div>{order.name}</div>
                </TableCell>
                {columns.includes('client') ? <TableCell>{order.clientName}</TableCell> : null}
                {columns.includes('project') ? <TableCell>{order.projectName}</TableCell> : null}
                {columns.includes('orderType') ? (
                  <TableCell>{order.orderTypeName}</TableCell>
                ) : null}
                {columns.includes('status') ? (
                  <TableCell>
                    <Chip
                      size="small"
                      label={orderStatusLabels[order.status as OrderStatus] ?? order.status}
                    />
                  </TableCell>
                ) : null}
                {columns.includes('priority') ? (
                  <TableCell>
                    <Chip
                      size="small"
                      variant="outlined"
                      label={orderPriorityLabels[order.priority as OrderPriority] ?? order.priority}
                    />
                  </TableCell>
                ) : null}
                {columns.includes('deadline') ? (
                  <TableCell>
                    <Typography
                      component="span"
                      sx={{ fontWeight: columns.includes('deadline') ? 600 : undefined }}
                    >
                      {order.deadline ?? '—'}
                    </Typography>
                  </TableCell>
                ) : null}
                {columns.includes('created') ? (
                  <TableCell>{formatCreated(order.createdAt)}</TableCell>
                ) : null}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Paper>
  )
}

function ProjectSection({ projects }: { projects: DashboardProject[] }) {
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Typography variant="h6" component="h3" sx={{ mb: 1 }}>
        Recent Projects
      </Typography>
      {projects.length === 0 ? (
        <Typography color="text.secondary">No recent Projects.</Typography>
      ) : (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Project</TableCell>
              <TableCell>Client</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Owner</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {projects.map((project) => (
              <TableRow key={project.id} hover>
                <TableCell>
                  <Link to={`/projects/${project.id}`}>{project.businessId}</Link>
                  <div>{project.name}</div>
                </TableCell>
                <TableCell>{project.clientName}</TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={projectStatusLabels[project.status as ProjectStatus] ?? project.status}
                  />
                </TableCell>
                <TableCell>{project.ownerName ?? '—'}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Paper>
  )
}

function formatCreated(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }
  return date.toLocaleDateString()
}
