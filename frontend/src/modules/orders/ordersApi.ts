import { apiRequest } from '../../api/apiClient.ts'

export type OrderStatus = 'draft' | 'active' | 'on_hold' | 'completed' | 'cancelled'
export type OrderPriority = 'low' | 'normal' | 'high' | 'urgent'

export const orderStatusLabels: Record<OrderStatus, string> = {
  draft: 'Draft',
  active: 'Active',
  on_hold: 'On Hold',
  completed: 'Completed',
  cancelled: 'Cancelled',
}

export const orderPriorityLabels: Record<OrderPriority, string> = {
  low: 'Low',
  normal: 'Normal',
  high: 'High',
  urgent: 'Urgent',
}

export type OrderClient = {
  id: string
  businessId: string
  name: string
}

export type OrderProjectSummary = {
  id: string
  businessId: string
  name: string
}

export type OrderProjectContext = OrderProjectSummary & {
  status: string
  deadline: string | null
}

export type OrderTypeSummary = {
  id: string
  name: string
  isActive: boolean
}

export type OrderPerson = {
  id: string
  fullName: string
  position: string | null
  isActive: boolean
}

export type OrderTeam = {
  owner: OrderPerson | null
  assignee: OrderPerson | null
  participants: OrderPerson[]
  observers: OrderPerson[]
}

export type ChecklistItem = {
  id: string
  text: string
  isCompleted: boolean
  sortOrder: number
}

export type ChecklistProgress = {
  completed: number
  total: number
}

export type FolderLink = {
  id: string
  name: string | null
  path: string
  sortOrder: number
}

export type OrderListItem = {
  id: string
  businessId: string
  name: string
  project: OrderProjectSummary
  client: OrderClient
  orderType: OrderTypeSummary
  status: OrderStatus
  priority: OrderPriority
  deadline: string | null
  previewImagePath: string | null
  sellingPrice?: number
  costPrice?: number
  profit?: number
}

export type OrderDetail = {
  id: string
  businessId: string
  project: OrderProjectContext
  client: OrderClient
  orderType: OrderTypeSummary
  name: string
  description: string | null
  status: OrderStatus
  priority: OrderPriority
  deadline: string | null
  previewImagePath: string | null
  team: OrderTeam
  calculatorConfigured: boolean
  checklistProgress: ChecklistProgress
  checklistItems: ChecklistItem[]
  folderLinks: FolderLink[]
  createdAt: string
  updatedAt: string | null
  sellingPrice?: number
  costPrice?: number
  profit?: number
}

export type OrderPage<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type OrderQuery = {
  search?: string
  projectId?: string
  clientId?: string
  orderTypeId?: string
  status?: OrderStatus | 'open'
  priority?: OrderPriority
  ownerEmployeeId?: string
  assigneeEmployeeId?: string
  deadlineFrom?: string
  deadlineTo?: string
  page?: number
  pageSize?: number
  sort?:
    | 'business_id'
    | 'name'
    | 'status'
    | 'priority'
    | 'deadline'
    | 'created_at'
    | 'selling_price'
    | 'cost_price'
  direction?: 'asc' | 'desc'
}

export type OrderInput = {
  projectId: string
  orderTypeId: string
  name: string
  description?: string
  priority?: OrderPriority
  deadline?: string
  previewImagePath?: string
}

export function listOrders(query: OrderQuery = {}): Promise<OrderPage<OrderListItem>> {
  return apiRequest<OrderPage<OrderListItem>>(`/api/orders${toQuery(query)}`)
}

export function getOrder(orderId: string): Promise<OrderDetail> {
  return apiRequest<OrderDetail>(`/api/orders/${orderId}`)
}

export function createOrder(input: OrderInput): Promise<OrderDetail> {
  return apiRequest<OrderDetail>('/api/orders', { method: 'POST', body: JSON.stringify(input) })
}

export function updateOrder(orderId: string, input: OrderInput): Promise<OrderDetail> {
  return apiRequest<OrderDetail>(`/api/orders/${orderId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function changeOrderStatus(orderId: string, status: OrderStatus): Promise<OrderDetail> {
  return apiRequest<OrderDetail>(`/api/orders/${orderId}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export function addChecklistItem(orderId: string, text: string): Promise<ChecklistItem> {
  return apiRequest<ChecklistItem>(`/api/orders/${orderId}/checklist-items`, {
    method: 'POST',
    body: JSON.stringify({ text }),
  })
}

export function updateChecklistItem(
  orderId: string,
  itemId: string,
  input: { text?: string; isCompleted?: boolean },
): Promise<ChecklistItem> {
  return apiRequest<ChecklistItem>(`/api/orders/${orderId}/checklist-items/${itemId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function deleteChecklistItem(orderId: string, itemId: string): Promise<void> {
  return apiRequest<void>(`/api/orders/${orderId}/checklist-items/${itemId}`, { method: 'DELETE' })
}

export function reorderChecklistItems(orderId: string, ids: string[]): Promise<ChecklistItem[]> {
  return apiRequest<ChecklistItem[]>(`/api/orders/${orderId}/checklist-items/reorder`, {
    method: 'POST',
    body: JSON.stringify({ ids }),
  })
}

export function addFolderLink(
  orderId: string,
  input: { name?: string; path: string },
): Promise<FolderLink> {
  return apiRequest<FolderLink>(`/api/orders/${orderId}/folder-links`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateFolderLink(
  orderId: string,
  linkId: string,
  input: { name?: string; path: string },
): Promise<FolderLink> {
  return apiRequest<FolderLink>(`/api/orders/${orderId}/folder-links/${linkId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function deleteFolderLink(orderId: string, linkId: string): Promise<void> {
  return apiRequest<void>(`/api/orders/${orderId}/folder-links/${linkId}`, { method: 'DELETE' })
}

export function reorderFolderLinks(orderId: string, ids: string[]): Promise<FolderLink[]> {
  return apiRequest<FolderLink[]>(`/api/orders/${orderId}/folder-links/reorder`, {
    method: 'POST',
    body: JSON.stringify({ ids }),
  })
}

export function money(value: number): string {
  return value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}

export function isPreviewUrl(path: string | null): path is string {
  return path !== null && (path.startsWith('http://') || path.startsWith('https://'))
}

function toQuery(query: OrderQuery): string {
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.projectId) params.set('project_id', query.projectId)
  if (query.clientId) params.set('client_id', query.clientId)
  if (query.orderTypeId) params.set('order_type_id', query.orderTypeId)
  if (query.status) params.set('status', query.status)
  if (query.priority) params.set('priority', query.priority)
  if (query.ownerEmployeeId) params.set('owner_employee_id', query.ownerEmployeeId)
  if (query.assigneeEmployeeId) params.set('assignee_employee_id', query.assigneeEmployeeId)
  if (query.deadlineFrom) params.set('deadline_from', query.deadlineFrom)
  if (query.deadlineTo) params.set('deadline_to', query.deadlineTo)
  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('page_size', String(query.pageSize))
  if (query.sort) params.set('sort', query.sort)
  if (query.direction) params.set('direction', query.direction)
  const text = params.toString()
  return text ? `?${text}` : ''
}
