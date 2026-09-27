import { apiRequest } from '../../api/apiClient.ts'

export type ProjectClient = {
  id: string
  businessId: string
  name: string
  isActive: boolean
}

export type ProjectPerson = {
  id: string
  fullName: string
  position: string | null
  isActive: boolean
}

export type ProjectTeam = {
  owner: ProjectPerson | null
  assignee: ProjectPerson | null
  participants: ProjectPerson[]
  observers: ProjectPerson[]
}

export type Project = {
  id: string
  businessId: string
  client: ProjectClient
  name: string
  description: string | null
  status: ProjectStatus
  startDate: string | null
  deadline: string | null
  team: ProjectTeam
  createdAt?: string
  updatedAt?: string | null
}

export type ProjectListItem = {
  id: string
  businessId: string
  name: string
  client: ProjectClient
  status: ProjectStatus
  startDate: string | null
  deadline: string | null
  owner: ProjectPerson | null
  assignee: ProjectPerson | null
}

export type ProjectOption = {
  id: string
  businessId: string
  name: string
  clientName: string
  status: ProjectStatus
}

export type ProjectPage<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type ProjectStatus = 'draft' | 'active' | 'on_hold' | 'completed' | 'cancelled'

export const projectStatusLabels: Record<ProjectStatus, string> = {
  draft: 'Draft',
  active: 'Active',
  on_hold: 'On Hold',
  completed: 'Completed',
  cancelled: 'Cancelled',
}

export type ProjectQuery = {
  search?: string
  status?: ProjectStatus | 'open'
  clientId?: string
  ownerEmployeeId?: string
  assigneeEmployeeId?: string
  startFrom?: string
  startTo?: string
  deadlineFrom?: string
  deadlineTo?: string
  page?: number
  pageSize?: number
  sort?: 'business_id' | 'name' | 'status' | 'start_date' | 'deadline' | 'created_at'
  direction?: 'asc' | 'desc'
  view?: 'list' | 'selector'
}

export type ProjectInput = {
  clientId: string
  name: string
  description?: string
  startDate?: string
  deadline?: string
}

export function listProjects(query: ProjectQuery = {}): Promise<ProjectPage<ProjectListItem>> {
  return apiRequest<ProjectPage<ProjectListItem>>(`/api/projects${toQuery(query)}`)
}

export function listOpenProjectOptions(search?: string): Promise<ProjectPage<ProjectOption>> {
  return apiRequest<ProjectPage<ProjectOption>>(
    `/api/projects${toQuery({ search, view: 'selector', pageSize: 50, sort: 'name', direction: 'asc' })}`,
  )
}

export function projectOptionLabel(project: ProjectOption): string {
  return `${project.businessId} — ${project.name} — ${project.clientName}`
}

export function getProject(projectId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}`)
}

export function createProject(input: ProjectInput): Promise<Project> {
  return apiRequest<Project>('/api/projects', { method: 'POST', body: JSON.stringify(input) })
}

export function updateProject(projectId: string, input: ProjectInput): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function changeProjectStatus(projectId: string, status: ProjectStatus): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/status`, {
    method: 'POST',
    body: JSON.stringify({ status }),
  })
}

export function setProjectOwner(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/owner`, {
    method: 'PUT',
    body: JSON.stringify({ employeeId }),
  })
}

export function clearProjectOwner(projectId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/owner`, { method: 'DELETE' })
}

export function setProjectAssignee(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/assignee`, {
    method: 'PUT',
    body: JSON.stringify({ employeeId }),
  })
}

export function clearProjectAssignee(projectId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/assignee`, { method: 'DELETE' })
}

export function addProjectParticipant(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/participants/${employeeId}`, {
    method: 'POST',
  })
}

export function removeProjectParticipant(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/participants/${employeeId}`, {
    method: 'DELETE',
  })
}

export function addProjectObserver(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/observers/${employeeId}`, {
    method: 'POST',
  })
}

export function removeProjectObserver(projectId: string, employeeId: string): Promise<Project> {
  return apiRequest<Project>(`/api/projects/${projectId}/observers/${employeeId}`, {
    method: 'DELETE',
  })
}

function toQuery(query: ProjectQuery): string {
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.status) params.set('status', query.status)
  if (query.clientId) params.set('client_id', query.clientId)
  if (query.ownerEmployeeId) params.set('owner_employee_id', query.ownerEmployeeId)
  if (query.assigneeEmployeeId) params.set('assignee_employee_id', query.assigneeEmployeeId)
  if (query.startFrom) params.set('start_from', query.startFrom)
  if (query.startTo) params.set('start_to', query.startTo)
  if (query.deadlineFrom) params.set('deadline_from', query.deadlineFrom)
  if (query.deadlineTo) params.set('deadline_to', query.deadlineTo)
  if (query.page) params.set('page', String(query.page))
  if (query.pageSize) params.set('page_size', String(query.pageSize))
  if (query.sort) params.set('sort', query.sort)
  if (query.direction) params.set('direction', query.direction)
  if (query.view) params.set('view', query.view)
  const text = params.toString()
  return text ? `?${text}` : ''
}
