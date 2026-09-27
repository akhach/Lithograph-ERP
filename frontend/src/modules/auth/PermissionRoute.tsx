import Alert from '@mui/material/Alert'
import type { ReactNode } from 'react'
import { EmployeesPage } from '../employees/EmployeesPage.tsx'
import { RolesPage } from './RolesPage.tsx'
import { useAuth } from './authContext.ts'
import { PermissionCodes } from './authTypes.ts'
import { UsersPage } from './UsersPage.tsx'

export function PermissionRoute({
  permission,
  anyOf,
  children,
}: {
  permission?: string
  anyOf?: readonly string[]
  children?: ReactNode
}) {
  const auth = useAuth()
  const allowed = anyOf
    ? anyOf.some((code) => auth.hasPermission(code))
    : Boolean(permission && auth.hasPermission(permission))
  if (!allowed) {
    return <Alert severity="warning">You do not have access to this page.</Alert>
  }
  if (children) {
    return children
  }
  if (permission === PermissionCodes.usersView) {
    return <UsersPage />
  }
  if (permission === PermissionCodes.employeesView) {
    return <EmployeesPage />
  }
  return <RolesPage />
}
