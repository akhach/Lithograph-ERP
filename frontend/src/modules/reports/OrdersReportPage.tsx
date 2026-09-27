import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import { Link, useSearchParams } from 'react-router'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { getOrdersReport } from './reportsApi.ts'
import {
  applyFilters,
  formatMoney,
  moneyCards,
  pageQuery,
  priorityLabel,
  sortQuery,
  statusLabel,
  useDraftFilters,
  useReportData,
  useReportOptions,
  type ReportField,
} from './reportQuery.ts'
import { ReportFilterBar, ReportScreen, SortHeader, SummaryCards } from './reportUi.tsx'

const fields = [
  'search',
  'fromDate',
  'toDate',
  'clientId',
  'projectId',
  'orderTypeId',
  'status',
  'priority',
  'ownerEmployeeId',
  'assigneeEmployeeId',
  'includeCancelled',
] as const satisfies readonly ReportField[]

export function OrdersReportPage() {
  const auth = useAuth()
  const [params, setParams] = useSearchParams()
  const [draft, setDraft] = useDraftFilters(params)
  const { options } = useReportOptions()
  const { data, error, loading, retry } = useReportData(getOrdersReport, params.toString())
  const selling = auth.hasPermission(PermissionCodes.ordersViewSellingPrice)
  const cost = auth.hasPermission(PermissionCodes.ordersViewCostPrice)
  const profit = selling && cost
  const sort = params.get('sort') ?? 'created_at'
  const direction = params.get('direction') === 'asc' ? 'asc' : 'desc'
  const page = data?.page ?? 1

  return (
    <ReportScreen
      title="Orders"
      note="Dates use each Order’s created date and include the whole day. Profit is selling price minus cost price. Totals cover every matching Order, not only this page."
      filters={
        <ReportFilterBar
          values={draft}
          fields={fields}
          options={options}
          onChange={setDraft}
          onApply={() => setParams(applyFilters(params, draft, fields))}
          onReset={() => setParams(new URLSearchParams())}
        />
      }
      summary={
        data ? (
          <SummaryCards
            cards={[
              { label: 'Orders', value: String(data.summary.totalOrders) },
              ...moneyCards(data.summary, selling, cost),
            ]}
          />
        ) : null
      }
      loading={loading}
      error={error}
      onRetry={retry}
      empty={
        data && data.items.length === 0 && !loading && !error
          ? 'No Orders match the selected filters.'
          : null
      }
      page={page}
      totalPages={data?.totalPages ?? 0}
      totalItems={data?.totalItems ?? 0}
      onPage={(next) => setParams(pageQuery(params, next))}
    >
      <Paper variant="outlined" sx={{ overflow: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>
                <SortHeader
                  label="Order"
                  field="order_business_id"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Name"
                  field="order_name"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Client"
                  field="client_name"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Project"
                  field="project_name"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Order type"
                  field="order_type"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Status"
                  field="status"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Priority"
                  field="priority"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Deadline"
                  field="deadline"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>
                <SortHeader
                  label="Created"
                  field="created_at"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              {selling ? (
                <TableCell align="right">
                  <SortHeader
                    label="Selling"
                    field="selling_price"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
              {cost ? (
                <TableCell align="right">
                  <SortHeader
                    label="Cost"
                    field="cost_price"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
              {profit ? (
                <TableCell align="right">
                  <SortHeader
                    label="Profit"
                    field="profit"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((row) => (
              <TableRow key={row.orderId} hover>
                <TableCell>
                  <Link to={`/orders/${row.orderId}`}>{row.orderBusinessId}</Link>
                </TableCell>
                <TableCell>{row.orderName}</TableCell>
                <TableCell>
                  <Link to={`/clients/${row.clientId}`}>{row.clientName}</Link>
                </TableCell>
                <TableCell>
                  <Link to={`/projects/${row.projectId}`}>{row.projectName}</Link>
                </TableCell>
                <TableCell>{row.orderTypeName}</TableCell>
                <TableCell>{statusLabel(row.status)}</TableCell>
                <TableCell>{priorityLabel(row.priority)}</TableCell>
                <TableCell>{row.deadline ?? '—'}</TableCell>
                <TableCell>{new Date(row.createdAt).toLocaleDateString()}</TableCell>
                {selling ? (
                  <TableCell align="right">{formatMoney(row.sellingPrice)}</TableCell>
                ) : null}
                {cost ? <TableCell align="right">{formatMoney(row.costPrice)}</TableCell> : null}
                {profit ? <TableCell align="right">{formatMoney(row.profit)}</TableCell> : null}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </ReportScreen>
  )
}
