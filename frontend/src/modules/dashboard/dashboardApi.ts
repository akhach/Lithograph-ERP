import { apiRequest } from '../../api/apiClient.ts'

export type DashboardSummary = {
  activeOrders: number
  urgentOrders: number
  dueSoonOrders: number
  overdueOrders: number
}

export type DashboardOrder = {
  id: string
  businessId: string
  name: string
  clientName: string
  projectName: string
  orderTypeName: string
  status: string
  priority: string
  deadline: string | null
  createdAt: string
}

export type DashboardProject = {
  id: string
  businessId: string
  name: string
  clientName: string
  status: string
  ownerName: string | null
}

export type DashboardResponse = {
  summary?: DashboardSummary
  activeOrders?: DashboardOrder[]
  urgentOrders?: DashboardOrder[]
  dueSoonOrders?: DashboardOrder[]
  overdueOrders?: DashboardOrder[]
  recentOrders?: DashboardOrder[]
  recentProjects?: DashboardProject[]
}

export function getDashboard(): Promise<DashboardResponse> {
  return apiRequest<DashboardResponse>('/api/dashboard')
}
