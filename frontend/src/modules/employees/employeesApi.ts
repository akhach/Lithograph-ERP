import { apiRequest } from '../../api/apiClient.ts'

export type LinkedUser = {
  id: string
  username: string
  isActive: boolean
}

export type Employee = {
  id: string
  fullName: string
  position: string | null
  phone: string | null
  email: string | null
  isActive: boolean
  linkedUser: LinkedUser | null
  createdAt?: string
  updatedAt?: string | null
}

export type EmployeeOption = {
  id: string
  fullName: string
  position: string | null
}

export type EmployeePage<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type EmployeeQuery = {
  search?: string
  isActive?: boolean
  hasUser?: boolean
  page?: number
  pageSize?: number
  sort?: 'full_name' | 'position' | 'is_active'
  direction?: 'asc' | 'desc'
  view?: 'list' | 'selector'
}

export function listEmployees(query: EmployeeQuery = {}): Promise<EmployeePage<Employee>> {
  return apiRequest<EmployeePage<Employee>>(`/api/employees${toQuery(query)}`)
}

export function listActiveEmployeeOptions(search?: string): Promise<EmployeePage<EmployeeOption>> {
  return apiRequest<EmployeePage<EmployeeOption>>(
    `/api/employees${toQuery({ search, isActive: true, view: 'selector', pageSize: 50, sort: 'full_name' })}`,
  )
}

export function employeeOptionLabel(employee: EmployeeOption): string {
  return employee.position ? `${employee.fullName} — ${employee.position}` : employee.fullName
}

export function getEmployee(employeeId: string): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}`)
}

export function createEmployee(input: {
  fullName: string
  position?: string
  phone?: string
  email?: string
  userId?: string
}): Promise<Employee> {
  return apiRequest<Employee>('/api/employees', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateEmployee(
  employeeId: string,
  input: { fullName: string; position?: string; phone?: string; email?: string },
): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function activateEmployee(employeeId: string): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}/activate`, { method: 'POST' })
}

export function deactivateEmployee(employeeId: string): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}/deactivate`, { method: 'POST' })
}

export function linkEmployeeUser(employeeId: string, userId: string): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}/link-user`, {
    method: 'POST',
    body: JSON.stringify({ userId }),
  })
}

export function unlinkEmployeeUser(employeeId: string): Promise<Employee> {
  return apiRequest<Employee>(`/api/employees/${employeeId}/user-link`, { method: 'DELETE' })
}

export function listAvailableUsers(): Promise<LinkedUser[]> {
  return apiRequest<LinkedUser[]>('/api/employees/available-users')
}

function toQuery(query: EmployeeQuery): string {
  const params = new URLSearchParams()
  if (query.search) {
    params.set('search', query.search)
  }
  if (query.isActive !== undefined) {
    params.set('is_active', String(query.isActive))
  }
  if (query.hasUser !== undefined) {
    params.set('has_user', String(query.hasUser))
  }
  if (query.page) {
    params.set('page', String(query.page))
  }
  if (query.pageSize) {
    params.set('page_size', String(query.pageSize))
  }
  if (query.sort) {
    params.set('sort', query.sort)
  }
  if (query.direction) {
    params.set('direction', query.direction)
  }
  if (query.view) {
    params.set('view', query.view)
  }
  const text = params.toString()
  return text ? `?${text}` : ''
}
