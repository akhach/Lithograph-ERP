import { apiRequest } from '../../api/apiClient.ts'

export type Client = {
  id: string
  businessId: string
  name: string
  contactPerson: string | null
  phone: string | null
  email: string | null
  address: string | null
  notes: string | null
  isActive: boolean
  createdAt?: string
  updatedAt?: string | null
}

export type ClientListItem = {
  id: string
  businessId: string
  name: string
  contactPerson: string | null
  phone: string | null
  email: string | null
  isActive: boolean
}

export type ClientOption = {
  id: string
  businessId: string
  name: string
}

export type ClientPage<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type ClientQuery = {
  search?: string
  isActive?: boolean
  page?: number
  pageSize?: number
  sort?: 'business_id' | 'name' | 'created_at' | 'is_active'
  direction?: 'asc' | 'desc'
  view?: 'list' | 'selector'
}

export type ClientInput = {
  name: string
  contactPerson?: string
  phone?: string
  email?: string
  address?: string
  notes?: string
}

export function listClients(query: ClientQuery = {}): Promise<ClientPage<ClientListItem>> {
  return apiRequest<ClientPage<ClientListItem>>(`/api/clients${toQuery(query)}`)
}

export function listActiveClientOptions(search?: string): Promise<ClientPage<ClientOption>> {
  return apiRequest<ClientPage<ClientOption>>(
    `/api/clients${toQuery({ search, isActive: true, view: 'selector', pageSize: 50, sort: 'name' })}`,
  )
}

export function clientOptionLabel(client: ClientOption): string {
  return `${client.businessId} — ${client.name}`
}

export function getClient(clientId: string): Promise<Client> {
  return apiRequest<Client>(`/api/clients/${clientId}`)
}

export function createClient(input: ClientInput): Promise<Client> {
  return apiRequest<Client>('/api/clients', { method: 'POST', body: JSON.stringify(input) })
}

export function updateClient(clientId: string, input: ClientInput): Promise<Client> {
  return apiRequest<Client>(`/api/clients/${clientId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export function activateClient(clientId: string): Promise<Client> {
  return apiRequest<Client>(`/api/clients/${clientId}/activate`, { method: 'POST' })
}

export function deactivateClient(clientId: string): Promise<Client> {
  return apiRequest<Client>(`/api/clients/${clientId}/deactivate`, { method: 'POST' })
}

function toQuery(query: ClientQuery): string {
  const params = new URLSearchParams()
  if (query.search) {
    params.set('search', query.search)
  }
  if (query.isActive !== undefined) {
    params.set('is_active', String(query.isActive))
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
