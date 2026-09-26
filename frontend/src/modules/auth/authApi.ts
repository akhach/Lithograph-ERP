import { apiRequest } from '../../api/apiClient.ts'
import type { CurrentUser, Permission, Role, SetupStatus, UserAccount } from './authTypes.ts'

export function getSetupStatus(): Promise<SetupStatus> {
  return apiRequest<SetupStatus>('/api/auth/setup-status')
}

export function setupDirector(password: string): Promise<void> {
  return apiRequest<void>('/api/auth/setup', {
    method: 'POST',
    body: JSON.stringify({ password }),
  })
}

export function login(username: string, password: string): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify({ username, password }),
  })
}

export function logout(): Promise<void> {
  return apiRequest<void>('/api/auth/logout', { method: 'POST' })
}

export function getCurrentUser(): Promise<CurrentUser> {
  return apiRequest<CurrentUser>('/api/auth/me')
}

export function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  return apiRequest<void>('/api/auth/change-password', {
    method: 'POST',
    body: JSON.stringify({ currentPassword, newPassword }),
  })
}

export function listUsers(): Promise<UserAccount[]> {
  return apiRequest<UserAccount[]>('/api/users')
}

export function createUser(input: {
  username: string
  password: string
  roleIds?: string[]
}): Promise<UserAccount> {
  return apiRequest<UserAccount>('/api/users', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export function updateUsername(userId: string, username: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/users/${userId}`, {
    method: 'PUT',
    body: JSON.stringify({ username }),
  })
}

export function activateUser(userId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/users/${userId}/activate`, { method: 'POST' })
}

export function deactivateUser(userId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/users/${userId}/deactivate`, { method: 'POST' })
}

export function resetPassword(userId: string, newPassword: string): Promise<void> {
  return apiRequest<void>(`/api/users/${userId}/reset-password`, {
    method: 'POST',
    body: JSON.stringify({ newPassword }),
  })
}

export function assignRole(userId: string, roleId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/users/${userId}/roles/${roleId}`, { method: 'POST' })
}

export function removeRole(userId: string, roleId: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`/api/users/${userId}/roles/${roleId}`, { method: 'DELETE' })
}

export function listRoles(): Promise<Role[]> {
  return apiRequest<Role[]>('/api/roles')
}

export function createRole(name: string, description: string): Promise<Role> {
  return apiRequest<Role>('/api/roles', {
    method: 'POST',
    body: JSON.stringify({ name, description: description || null }),
  })
}

export function updateRole(roleId: string, name: string, description: string): Promise<Role> {
  return apiRequest<Role>(`/api/roles/${roleId}`, {
    method: 'PUT',
    body: JSON.stringify({ name, description: description || null }),
  })
}

export function setRolePermissions(roleId: string, permissionIds: string[]): Promise<Role> {
  return apiRequest<Role>(`/api/roles/${roleId}/permissions`, {
    method: 'PUT',
    body: JSON.stringify({ permissionIds }),
  })
}

export function listPermissions(): Promise<Permission[]> {
  return apiRequest<Permission[]>('/api/permissions')
}
