import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import FormControlLabel from '@mui/material/FormControlLabel'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TableSortLabel from '@mui/material/TableSortLabel'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { type ReactNode } from 'react'
import { orderPriorityLabels, orderStatusLabels } from '../orders/ordersApi.ts'
import { projectStatusLabels } from '../projects/projectsApi.ts'
import { type ReportField, type ReportFilters } from './reportQuery.ts'
import { type ReportFilterOptions } from './reportsApi.ts'

export function ReportScreen({
  title,
  note,
  filters,
  summary,
  loading,
  error,
  onRetry,
  empty,
  page,
  totalPages,
  totalItems,
  onPage,
  children,
}: {
  title: string
  note: string
  filters: ReactNode
  summary: ReactNode
  loading: boolean
  error: string | null
  onRetry: () => void
  empty: string | null
  page: number
  totalPages: number
  totalItems: number
  onPage: (page: number) => void
  children: ReactNode
}) {
  return (
    <Stack spacing={2}>
      <Box>
        <Typography variant="h5" component="h2">
          {title}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {note}
        </Typography>
      </Box>
      {filters}
      {error ? (
        <Alert
          severity="error"
          action={
            <Button color="inherit" size="small" onClick={onRetry}>
              Retry
            </Button>
          }
        >
          {error}
        </Alert>
      ) : null}
      {loading ? <Alert severity="info">Loading report…</Alert> : null}
      {summary}
      {empty ? <Typography color="text.secondary">{empty}</Typography> : children}
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
        <Button size="small" disabled={page <= 1} onClick={() => onPage(page - 1)}>
          Previous
        </Button>
        <Button size="small" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>
          Next
        </Button>
        <Typography variant="body2" color="text.secondary">
          Page {totalPages === 0 ? 0 : page} of {totalPages} · {totalItems} rows
        </Typography>
      </Stack>
    </Stack>
  )
}

export function SummaryCards({ cards }: { cards: { label: string; value: string }[] }) {
  return (
    <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
      {cards.map((card) => (
        <Paper key={card.label} variant="outlined" sx={{ px: 2, py: 1, minWidth: 140 }}>
          <Typography variant="caption" color="text.secondary">
            {card.label}
          </Typography>
          <Typography variant="h6">{card.value}</Typography>
        </Paper>
      ))}
    </Stack>
  )
}

export function SortHeader({
  label,
  field,
  sort,
  direction,
  onSort,
}: {
  label: string
  field: string
  sort: string
  direction: 'asc' | 'desc'
  onSort: (field: string) => void
}) {
  return (
    <TableSortLabel
      active={sort === field}
      direction={sort === field ? direction : 'asc'}
      onClick={() => onSort(field)}
    >
      {label}
    </TableSortLabel>
  )
}

export function ReportFilterBar({
  values,
  fields,
  options,
  onChange,
  onApply,
  onReset,
}: {
  values: ReportFilters
  fields: readonly ReportField[]
  options: ReportFilterOptions | null
  onChange: (values: ReportFilters) => void
  onApply: () => void
  onReset: () => void
}) {
  const set = (patch: Partial<ReportFilters>) => onChange({ ...values, ...patch })
  const shown = new Set(fields)
  return (
    <Paper variant="outlined" sx={{ p: 2 }}>
      <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        {shown.has('groupBy') ? (
          <TextField
            select
            size="small"
            label="Group by"
            value={values.groupBy}
            onChange={(event) => set({ groupBy: event.target.value })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="category">Category</MenuItem>
            <MenuItem value="supplier">Supplier</MenuItem>
            <MenuItem value="order">Order</MenuItem>
            <MenuItem value="project">Project</MenuItem>
            <MenuItem value="client">Client</MenuItem>
            <MenuItem value="order_type">Order Type</MenuItem>
          </TextField>
        ) : null}
        {shown.has('search') ? (
          <TextField
            size="small"
            label="Search"
            value={values.search}
            onChange={(event) => set({ search: event.target.value })}
            sx={{ minWidth: 180 }}
          />
        ) : null}
        {shown.has('fromDate') ? (
          <DateField
            label="From"
            value={values.fromDate}
            onChange={(fromDate) => set({ fromDate })}
          />
        ) : null}
        {shown.has('toDate') ? (
          <DateField label="To" value={values.toDate} onChange={(toDate) => set({ toDate })} />
        ) : null}
        {shown.has('fromExpenseDate') ? (
          <DateField
            label="Expense from"
            value={values.fromExpenseDate}
            onChange={(fromExpenseDate) => set({ fromExpenseDate })}
          />
        ) : null}
        {shown.has('toExpenseDate') ? (
          <DateField
            label="Expense to"
            value={values.toExpenseDate}
            onChange={(toExpenseDate) => set({ toExpenseDate })}
          />
        ) : null}
        {shown.has('clientId') ? (
          <TextField
            select
            size="small"
            label="Client"
            value={values.clientId}
            onChange={(event) => set({ clientId: event.target.value })}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.clients.map((client) => (
              <MenuItem key={client.id} value={client.id}>
                {client.businessId} · {client.name}
                {client.isActive ? '' : ' (inactive)'}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('projectId') ? (
          <TextField
            select
            size="small"
            label="Project"
            value={values.projectId}
            onChange={(event) => set({ projectId: event.target.value })}
            sx={{ minWidth: 180 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.projects.map((project) => (
              <MenuItem key={project.id} value={project.id}>
                {project.businessId} · {project.name}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('orderId') ? (
          <TextField
            select
            size="small"
            label="Order"
            value={values.orderId}
            onChange={(event) => set({ orderId: event.target.value })}
            sx={{ minWidth: 200 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.orders.map((order) => (
              <MenuItem key={order.id} value={order.id}>
                {order.businessId} · {order.name}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('orderTypeId') ? (
          <TextField
            select
            size="small"
            label="Order type"
            value={values.orderTypeId}
            onChange={(event) => set({ orderTypeId: event.target.value })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.orderTypes.map((type) => (
              <MenuItem key={type.id} value={type.id}>
                {type.name}
                {type.isActive ? '' : ' (inactive)'}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('status') ? (
          <TextField
            select
            size="small"
            label="Order status"
            value={values.status}
            onChange={(event) => set({ status: event.target.value })}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">All</MenuItem>
            {Object.entries(orderStatusLabels).map(([value, label]) => (
              <MenuItem key={value} value={value}>
                {label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('projectStatus') ? (
          <TextField
            select
            size="small"
            label="Project status"
            value={values.projectStatus}
            onChange={(event) => set({ projectStatus: event.target.value })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All</MenuItem>
            {Object.entries(projectStatusLabels).map(([value, label]) => (
              <MenuItem key={value} value={value}>
                {label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('priority') ? (
          <TextField
            select
            size="small"
            label="Priority"
            value={values.priority}
            onChange={(event) => set({ priority: event.target.value })}
            sx={{ minWidth: 140 }}
          >
            <MenuItem value="">All</MenuItem>
            {Object.entries(orderPriorityLabels).map(([value, label]) => (
              <MenuItem key={value} value={value}>
                {label}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('ownerEmployeeId') ? (
          <TextField
            select
            size="small"
            label="Owner"
            value={values.ownerEmployeeId}
            onChange={(event) => set({ ownerEmployeeId: event.target.value })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.employees.map((employee) => (
              <MenuItem key={employee.id} value={employee.id}>
                {employee.fullName}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('assigneeEmployeeId') ? (
          <TextField
            select
            size="small"
            label="Assignee"
            value={values.assigneeEmployeeId}
            onChange={(event) => set({ assigneeEmployeeId: event.target.value })}
            sx={{ minWidth: 160 }}
          >
            <MenuItem value="">All</MenuItem>
            {options?.employees.map((employee) => (
              <MenuItem key={employee.id} value={employee.id}>
                {employee.fullName}
              </MenuItem>
            ))}
          </TextField>
        ) : null}
        {shown.has('category') ? (
          <TextField
            size="small"
            label="Category"
            value={values.category}
            onChange={(event) => set({ category: event.target.value })}
            sx={{ minWidth: 150 }}
          />
        ) : null}
        {shown.has('supplier') ? (
          <TextField
            size="small"
            label="Supplier"
            value={values.supplier}
            onChange={(event) => set({ supplier: event.target.value })}
            sx={{ minWidth: 150 }}
          />
        ) : null}
        <Button variant="contained" onClick={onApply}>
          Apply
        </Button>
        <Button onClick={onReset}>Reset</Button>
      </Stack>
      {shown.has('includeCancelled') ? (
        <FormControlLabel
          sx={{ mt: 1 }}
          control={
            <Checkbox
              checked={values.includeCancelled}
              onChange={(event) => set({ includeCancelled: event.target.checked })}
            />
          }
          label="Include cancelled Orders. Cancelled Orders are excluded until this is selected. Choosing status Cancelled still shows those Orders."
        />
      ) : null}
    </Paper>
  )
}

function DateField({
  label,
  value,
  onChange,
}: {
  label: string
  value: string
  onChange: (value: string) => void
}) {
  return (
    <TextField
      label={label}
      type="date"
      size="small"
      slotProps={{ inputLabel: { shrink: true } }}
      value={value}
      onChange={(event) => onChange(event.target.value)}
    />
  )
}
