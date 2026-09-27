import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from '../layouts/AppLayout.tsx'
import { AuthProvider } from '../modules/auth/AuthProvider.tsx'
import { HomePage } from '../modules/auth/HomePage.tsx'
import { LoginPage } from '../modules/auth/LoginPage.tsx'
import { PermissionRoute } from '../modules/auth/PermissionRoute.tsx'
import { SetupPage } from '../modules/auth/SetupPage.tsx'
import { PermissionCodes } from '../modules/auth/authTypes.ts'
import { SessionStatus } from '../modules/auth/SessionStatus.tsx'

export const appRouter = createBrowserRouter([
  {
    element: <AuthProvider />,
    children: [
      { path: '/setup', element: <SetupPage /> },
      { path: '/login', element: <LoginPage /> },
      {
        element: (
          <SessionStatus expect="authenticated">
            <AppLayout />
          </SessionStatus>
        ),
        children: [
          { index: true, element: <HomePage /> },
          {
            path: 'users',
            element: <PermissionRoute permission={PermissionCodes.usersView} />,
          },
          {
            path: 'roles',
            element: <PermissionRoute permission={PermissionCodes.rolesView} />,
          },
          {
            path: 'admin/employees',
            element: <PermissionRoute permission={PermissionCodes.employeesView} />,
          },
          { path: '*', element: <Navigate to="/" replace /> },
        ],
      },
    ],
  },
])
