import Alert from '@mui/material/Alert'
import { RolesPage } from './RolesPage.tsx'
import { useAuth } from './authContext.ts'
import { PermissionCodes } from './authTypes.ts'
import { UsersPage } from './UsersPage.tsx'
import { EmployeesPage } from '../employees/EmployeesPage.tsx'

export function PermissionRoute({ permission }: { permission: string }) {
  const auth = useAuth()
  if (!auth.hasPermission(permission)) {
    return <Alert severity="warning">You do not have access to this page.</Alert>
  }
  if (permission === PermissionCodes.usersView) {
    return <UsersPage />
  }
  if (permission === PermissionCodes.employeesView) {
    return <EmployeesPage />
  }
  return <RolesPage />
}
