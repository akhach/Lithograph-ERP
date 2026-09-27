export type CurrentUser = {
  id: string
  username: string
  roles: string[]
  permissions: string[]
  linkedEmployee: string | null
}

export type SetupStatus = {
  requiresSetup: boolean
}

export type RoleSummary = {
  id: string
  name: string
  isSystem: boolean
}

export type UserAccount = {
  id: string
  username: string
  isActive: boolean
  lastLoginAt: string | null
  roles: RoleSummary[]
}

export type Permission = {
  id: string
  code: string
  name: string
  description: string | null
  module: string
}

export type Role = {
  id: string
  name: string
  description: string | null
  isSystem: boolean
  permissions: Permission[]
}

export const PermissionCodes = {
  usersView: 'users.view',
  usersCreate: 'users.create',
  usersEdit: 'users.edit',
  usersActivate: 'users.activate',
  usersResetPassword: 'users.reset_password',
  usersManageRoles: 'users.manage_roles',
  rolesView: 'roles.view',
  rolesCreate: 'roles.create',
  rolesEdit: 'roles.edit',
  rolesManagePermissions: 'roles.manage_permissions',
  employeesView: 'employees.view',
  employeesCreate: 'employees.create',
  employeesEdit: 'employees.edit',
  employeesActivate: 'employees.activate',
  employeesLinkUser: 'employees.link_user',
} as const
