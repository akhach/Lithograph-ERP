import { apiRequest } from '../../api/apiClient.ts'

export type ReportClientOption = {
  id: string
  businessId: string
  name: string
  isActive: boolean
}

export type ReportProjectOption = {
  id: string
  businessId: string
  name: string
  clientId: string
  status: string
}

export type ReportOrderTypeOption = {
  id: string
  name: string
  isActive: boolean
}

export type ReportEmployeeOption = {
  id: string
  fullName: string
}

export type ReportOrderOption = {
  id: string
  businessId: string
  name: string
  projectId: string
}

export type ReportFilterOptions = {
  clients: ReportClientOption[]
  projects: ReportProjectOption[]
  orderTypes: ReportOrderTypeOption[]
  employees: ReportEmployeeOption[]
  orders: ReportOrderOption[]
}

export type ReportPage<TItem, TSummary> = {
  items: TItem[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
  summary: TSummary
}

export type MoneySummary = {
  sellingTotal?: number
  costTotal?: number
  profitTotal?: number
}

export type OrderReportRow = {
  orderId: string
  orderBusinessId: string
  orderName: string
  clientId: string
  clientBusinessId: string
  clientName: string
  projectId: string
  projectBusinessId: string
  projectName: string
  orderTypeId: string
  orderTypeName: string
  orderTypeIsActive: boolean
  status: string
  priority: string
  deadline: string | null
  createdAt: string
  sellingPrice?: number
  costPrice?: number
  profit?: number
}

export type OrderReportSummary = MoneySummary & { totalOrders: number }

export type ProjectReportRow = {
  projectId: string
  projectBusinessId: string
  projectName: string
  clientId: string
  clientBusinessId: string
  clientName: string
  status: string
  ownerEmployeeId: string | null
  ownerName: string | null
  assigneeEmployeeId: string | null
  assigneeName: string | null
  orderCount: number
  sellingTotal?: number
  costTotal?: number
  profit?: number
}

export type ProjectReportSummary = MoneySummary & { totalProjects: number; totalOrders: number }

export type ClientReportRow = {
  clientId: string
  clientBusinessId: string
  clientName: string
  isActive: boolean
  projectCount: number
  orderCount: number
  sellingTotal?: number
  costTotal?: number
  profit?: number
}

export type ClientReportSummary = MoneySummary & {
  totalClients: number
  totalProjects: number
  totalOrders: number
}

export type OrderTypeReportRow = {
  orderTypeId: string
  orderTypeName: string
  isActive: boolean
  orderCount: number
  sellingTotal?: number
  costTotal?: number
  profit?: number
}

export type OrderTypeReportSummary = MoneySummary & { totalOrderTypes: number; totalOrders: number }

export type CostReportRow = {
  groupKey: string
  label: string | null
  orderId?: string
  orderBusinessId?: string
  orderName?: string
  projectId?: string
  projectBusinessId?: string
  projectName?: string
  clientId?: string
  clientBusinessId?: string
  clientName?: string
  orderTypeId?: string
  orderTypeName?: string
  orderTypeIsActive?: boolean
  costItemCount: number
  totalAmount: number
}

export type CostReportSummary = {
  totalGroups: number
  costItemCount: number
  totalAmount: number
}

export function listReportOptions(): Promise<ReportFilterOptions> {
  return apiRequest<ReportFilterOptions>('/api/reports/options')
}

export function getOrdersReport(
  query: string,
): Promise<ReportPage<OrderReportRow, OrderReportSummary>> {
  return apiRequest(`/api/reports/orders${query}`)
}

export function getProjectsReport(
  query: string,
): Promise<ReportPage<ProjectReportRow, ProjectReportSummary>> {
  return apiRequest(`/api/reports/projects${query}`)
}

export function getClientsReport(
  query: string,
): Promise<ReportPage<ClientReportRow, ClientReportSummary>> {
  return apiRequest(`/api/reports/clients${query}`)
}

export function getOrderTypesReport(
  query: string,
): Promise<ReportPage<OrderTypeReportRow, OrderTypeReportSummary>> {
  return apiRequest(`/api/reports/order-types${query}`)
}

export function getCostsReport(
  query: string,
): Promise<ReportPage<CostReportRow, CostReportSummary>> {
  return apiRequest(`/api/reports/costs${query}`)
}
