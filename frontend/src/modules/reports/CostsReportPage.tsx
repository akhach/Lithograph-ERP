import Alert from '@mui/material/Alert'
import Paper from '@mui/material/Paper'
import Table from '@mui/material/Table'
import TableBody from '@mui/material/TableBody'
import TableCell from '@mui/material/TableCell'
import TableHead from '@mui/material/TableHead'
import TableRow from '@mui/material/TableRow'
import { Link, useSearchParams } from 'react-router'
import { useAuth } from '../auth/authContext.ts'
import { PermissionCodes } from '../auth/authTypes.ts'
import { money } from '../orders/ordersApi.ts'
import { getCostsReport } from './reportsApi.ts'
import {
  applyFilters,
  pageQuery,
  sortQuery,
  useDraftFilters,
  useReportData,
  useReportOptions,
  type ReportField,
} from './reportQuery.ts'
import { ReportFilterBar, ReportScreen, SortHeader, SummaryCards } from './reportUi.tsx'

const fields = [
  'groupBy',
  'fromExpenseDate',
  'toExpenseDate',
  'clientId',
  'projectId',
  'orderId',
  'orderTypeId',
  'category',
  'supplier',
  'status',
  'includeCancelled',
] as const satisfies readonly ReportField[]

export function CostsReportPage() {
  const auth = useAuth()
  if (!auth.hasPermission(PermissionCodes.calculatorViewCosts)) {
    return <Alert severity="warning">You do not have permission to access this report data.</Alert>
  }
  return <CostsReport />
}

function CostsReport() {
  const [params, setParams] = useSearchParams()
  const [draft, setDraft] = useDraftFilters(params)
  const { options } = useReportOptions()
  const { data, error, loading, retry } = useReportData(getCostsReport, params.toString())
  const group = params.get('group_by') ?? 'category'
  const sort = params.get('sort') ?? 'total_amount'
  const direction = params.get('direction') === 'asc' ? 'asc' : 'desc'

  return (
    <ReportScreen
      title="Costs"
      note="Amounts come from Cost Items. Expense dates are inclusive, and Cost Items without an expense date are left out of a date range. Supplier and category text is grouped as entered."
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
              { label: 'Groups', value: String(data.summary.totalGroups) },
              { label: 'Cost items', value: String(data.summary.costItemCount) },
              { label: 'Amount', value: money(data.summary.totalAmount) },
            ]}
          />
        ) : null
      }
      loading={loading}
      error={error}
      onRetry={retry}
      empty={
        data && data.items.length === 0 && !loading && !error
          ? 'No Costs match the selected filters.'
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
              {group === 'category' || group === 'supplier' ? (
                <TableCell>
                  <SortHeader
                    label={group === 'supplier' ? 'Supplier' : 'Category'}
                    field="label"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
              {group === 'order' ? (
                <>
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
                      label="Project"
                      field="project_name"
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
                </>
              ) : null}
              {group === 'project' ? (
                <>
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
                      label="Client"
                      field="client_name"
                      sort={sort}
                      direction={direction}
                      onSort={(field) => setParams(sortQuery(params, field))}
                    />
                  </TableCell>
                </>
              ) : null}
              {group === 'client' ? (
                <TableCell>
                  <SortHeader
                    label="Client"
                    field="client_name"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
              {group === 'order_type' ? (
                <TableCell>
                  <SortHeader
                    label="Order type"
                    field="order_type"
                    sort={sort}
                    direction={direction}
                    onSort={(field) => setParams(sortQuery(params, field))}
                  />
                </TableCell>
              ) : null}
              <TableCell align="right">
                <SortHeader
                  label="Items"
                  field="cost_item_count"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
              <TableCell align="right">
                <SortHeader
                  label="Amount"
                  field="total_amount"
                  sort={sort}
                  direction={direction}
                  onSort={(field) => setParams(sortQuery(params, field))}
                />
              </TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {data?.items.map((row) => (
              <TableRow key={row.groupKey || 'none'} hover>
                {group === 'category' ? <TableCell>{row.label}</TableCell> : null}
                {group === 'supplier' ? <TableCell>{row.label || 'No Supplier'}</TableCell> : null}
                {group === 'order' ? (
                  <>
                    <TableCell>
                      {row.orderId ? (
                        <Link to={`/orders/${row.orderId}`}>{row.orderBusinessId}</Link>
                      ) : null}
                      <div>{row.orderName}</div>
                    </TableCell>
                    <TableCell>
                      {row.projectId ? (
                        <Link to={`/projects/${row.projectId}`}>{row.projectName}</Link>
                      ) : (
                        row.projectName
                      )}
                    </TableCell>
                    <TableCell>
                      {row.clientId ? (
                        <Link to={`/clients/${row.clientId}`}>{row.clientName}</Link>
                      ) : (
                        row.clientName
                      )}
                    </TableCell>
                  </>
                ) : null}
                {group === 'project' ? (
                  <>
                    <TableCell>
                      {row.projectId ? (
                        <Link to={`/projects/${row.projectId}`}>{row.projectBusinessId}</Link>
                      ) : null}
                      <div>{row.projectName}</div>
                    </TableCell>
                    <TableCell>
                      {row.clientId ? (
                        <Link to={`/clients/${row.clientId}`}>{row.clientName}</Link>
                      ) : (
                        row.clientName
                      )}
                    </TableCell>
                  </>
                ) : null}
                {group === 'client' ? (
                  <TableCell>
                    {row.clientId ? (
                      <Link to={`/clients/${row.clientId}`}>{row.clientBusinessId}</Link>
                    ) : null}
                    <div>{row.clientName}</div>
                  </TableCell>
                ) : null}
                {group === 'order_type' ? (
                  <TableCell>
                    {row.orderTypeName}
                    {row.orderTypeIsActive === false ? ' (inactive)' : ''}
                  </TableCell>
                ) : null}
                <TableCell align="right">{row.costItemCount}</TableCell>
                <TableCell align="right">{money(row.totalAmount)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
    </ReportScreen>
  )
}
