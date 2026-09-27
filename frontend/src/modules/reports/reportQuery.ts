import { useEffect, useState } from 'react'
import { messageOf } from '../auth/apiMessages.ts'
import {
  money,
  orderPriorityLabels,
  orderStatusLabels,
  type OrderPriority,
  type OrderStatus,
} from '../orders/ordersApi.ts'
import { projectStatusLabels, type ProjectStatus } from '../projects/projectsApi.ts'
import { listReportOptions, type ReportFilterOptions } from './reportsApi.ts'

export type ReportFilters = {
  search: string
  fromDate: string
  toDate: string
  fromExpenseDate: string
  toExpenseDate: string
  clientId: string
  projectId: string
  orderId: string
  orderTypeId: string
  status: string
  projectStatus: string
  priority: string
  ownerEmployeeId: string
  assigneeEmployeeId: string
  category: string
  supplier: string
  groupBy: string
  includeCancelled: boolean
}

export type ReportField = keyof ReportFilters

const queryNames: Record<ReportField, string> = {
  search: 'search',
  fromDate: 'from_date',
  toDate: 'to_date',
  fromExpenseDate: 'from_expense_date',
  toExpenseDate: 'to_expense_date',
  clientId: 'client_id',
  projectId: 'project_id',
  orderId: 'order_id',
  orderTypeId: 'order_type_id',
  status: 'status',
  projectStatus: 'project_status',
  priority: 'priority',
  ownerEmployeeId: 'owner_employee_id',
  assigneeEmployeeId: 'assignee_employee_id',
  category: 'category',
  supplier: 'supplier',
  groupBy: 'group_by',
  includeCancelled: 'include_cancelled',
}

export const emptyFilters: ReportFilters = {
  search: '',
  fromDate: '',
  toDate: '',
  fromExpenseDate: '',
  toExpenseDate: '',
  clientId: '',
  projectId: '',
  orderId: '',
  orderTypeId: '',
  status: '',
  projectStatus: '',
  priority: '',
  ownerEmployeeId: '',
  assigneeEmployeeId: '',
  category: '',
  supplier: '',
  groupBy: 'category',
  includeCancelled: false,
}

export function readFilters(params: URLSearchParams): ReportFilters {
  const next = { ...emptyFilters }
  for (const field of Object.keys(queryNames) as ReportField[]) {
    const value = params.get(queryNames[field])
    if (!value) {
      continue
    }
    if (field === 'includeCancelled') {
      next.includeCancelled = value === 'true'
    } else {
      next[field] = value
    }
  }
  return next
}

export function applyFilters(
  current: URLSearchParams,
  values: ReportFilters,
  fields: readonly ReportField[],
): URLSearchParams {
  const next = new URLSearchParams()
  for (const key of ['sort', 'direction', 'page_size']) {
    const value = current.get(key)
    if (value) {
      next.set(key, value)
    }
  }
  for (const field of fields) {
    const value = values[field]
    if (typeof value === 'boolean') {
      if (value) {
        next.set(queryNames[field], 'true')
      }
    } else if (value) {
      next.set(queryNames[field], value)
    }
  }
  return next
}

export function pageQuery(params: URLSearchParams, page: number): URLSearchParams {
  const next = new URLSearchParams(params)
  if (page <= 1) {
    next.delete('page')
  } else {
    next.set('page', String(page))
  }
  return next
}

export function sortQuery(params: URLSearchParams, sort: string): URLSearchParams {
  const next = new URLSearchParams(params)
  const current = next.get('sort')
  const direction = next.get('direction') ?? 'asc'
  if (current === sort) {
    next.set('direction', direction === 'asc' ? 'desc' : 'asc')
  } else {
    next.set('sort', sort)
    next.set('direction', 'asc')
  }
  next.delete('page')
  return next
}

export function useReportOptions() {
  const [options, setOptions] = useState<ReportFilterOptions | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    async function load() {
      await Promise.resolve()
      if (!active) {
        return
      }
      try {
        const result = await listReportOptions()
        if (active) {
          setOptions(result)
        }
      } catch (caught) {
        if (active) {
          setError(messageOf(caught))
        }
      }
    }
    void load()
    return () => {
      active = false
    }
  }, [])

  return { options, error }
}

export function useDraftFilters(params: URLSearchParams) {
  const search = params.toString()
  const [draft, setDraft] = useState(() => readFilters(params))
  const [seen, setSeen] = useState(search)
  if (seen !== search) {
    setSeen(search)
    setDraft(readFilters(params))
  }
  return [draft, setDraft] as const
}

export function useReportData<T>(load: (query: string) => Promise<T>, search: string) {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    async function run() {
      await Promise.resolve()
      if (!active) {
        return
      }
      setLoading(true)
      setError(null)
      try {
        const result = await load(search ? `?${search}` : '')
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
    void run()
    return () => {
      active = false
    }
  }, [load, search, reloadKey])

  return { data, error, loading, retry: () => setReloadKey((value) => value + 1) }
}

export function moneyCards(
  summary: { sellingTotal?: number; costTotal?: number; profitTotal?: number },
  selling: boolean,
  cost: boolean,
): { label: string; value: string }[] {
  const cards: { label: string; value: string }[] = []
  if (selling) {
    cards.push({ label: 'Selling', value: money(summary.sellingTotal ?? 0) })
  }
  if (cost) {
    cards.push({ label: 'Cost', value: money(summary.costTotal ?? 0) })
  }
  if (selling && cost) {
    cards.push({ label: 'Profit', value: money(summary.profitTotal ?? 0) })
  }
  return cards
}

export function statusLabel(status: string): string {
  return (
    orderStatusLabels[status as OrderStatus] ??
    projectStatusLabels[status as ProjectStatus] ??
    status
  )
}

export function priorityLabel(priority: string): string {
  return orderPriorityLabels[priority as OrderPriority] ?? priority
}

export function formatMoney(value: number | undefined): string {
  return value === undefined ? '—' : money(value)
}
