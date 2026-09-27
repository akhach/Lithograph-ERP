import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import { Link, useSearchParams } from 'react-router'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { getClientsReport } from './reportsApi.ts'
import {
  applyFilters,
  formatMoney,
  moneyCards,
  pageQuery,
  sortQuery,
  useDraftFilters,
  useReportData,
  useReportOptions,
  type ReportField,
} from './reportQuery.ts'
import { ReportFilterBar, ReportScreen, SortHeader, SummaryCards } from './reportUi.tsx'

const fields = [
  'fromDate',
  'toDate',
  'clientId',
  'projectStatus',
  'orderTypeId',
  'status',
  'ownerEmployeeId',
  'assigneeEmployeeId',
  'includeCancelled',
] as const satisfies readonly ReportField[]

export function ClientsReportPage() {
  const auth = useAuth()
  const [params, setParams] = useSearchParams()
  const [draft, setDraft] = useDraftFilters(params)
  const { options } = useReportOptions()
  const { data, error, loading, retry } = useReportData(getClientsReport, params.toString())
  const selling = auth.hasPermission(PermissionCodes.ordersViewSellingPrice)
  const cost = auth.hasPermission(PermissionCodes.ordersViewCostPrice)
  const profit = selling && cost
  const sort = params.get('sort') ?? 'client_name'
  const direction = params.get('direction') === 'desc' ? 'desc' : 'asc'

  return (
    <ReportScreen
      title="Clients"
      note="Totals follow Client, then Projects, then matching Orders. Project count is the number of distinct Projects that contain those Orders. Inactive Clients stay visible."
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
              { label: 'Clients', value: String(data.summary.totalClients) },
              { label: 'Projects', value: String(data.summary.totalProjects) },
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
          ? 'No Clients match the selected filters.'
          : null
      }
      page={data?.page ?? 1}
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
                  label="Client"
                  field="client_name"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell>Active</TableCell>
              <TableCell align="right">
                <SortHeader
                  label="Projects"
                  field="project_count"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell align="right">
                <SortHeader
                  label="Orders"
                  field="order_count"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              {selling ? (
                <TableCell align="right">
                  <SortHeader
                    label="Selling"
                    field="selling_total"
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
                    field="cost_total"
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
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((row) => (
              <TableRow key={row.clientId} hover>
                <TableCell>
                  <Link to={`/clients/${row.clientId}`}>{row.clientBusinessId}</Link>
                  <div>{row.clientName}</div>
                </TableCell>
                <TableCell>{row.isActive ? 'Active' : 'Inactive'}</TableCell>
                <TableCell align="right">{row.projectCount}</TableCell>
                <TableCell align="right">{row.orderCount}</TableCell>
                {selling ? (
                  <TableCell align="right">{formatMoney(row.sellingTotal)}</TableCell>
                ) : null}
                {cost ? <TableCell align="right">{formatMoney(row.costTotal)}</TableCell> : null}
                {profit ? <TableCell align="right">{formatMoney(row.profit)}</TableCell> : null}
                <TableCell>
                  <Link to={`/reports/orders?client_id=${row.clientId}`}>View orders</Link>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </ReportScreen>
  )
}
