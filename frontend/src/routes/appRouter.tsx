import { createBrowserRouter, Navigate } from 'react-router'
import { AppLayout } from '../layouts/AppLayout.tsx'
import { AuthProvider } from '../modules/auth/AuthProvider.tsx'
import { LoginPage } from '../modules/auth/LoginPage.tsx'
import { DashboardPage } from '../modules/dashboard/DashboardPage.tsx'
import { PermissionRoute } from '../modules/auth/PermissionRoute.tsx'
import { SetupPage } from '../modules/auth/SetupPage.tsx'
import { PermissionCodes } from '../modules/auth/authTypes.ts'
import { SessionStatus } from '../modules/auth/SessionStatus.tsx'
import { ClientDetailPage } from '../modules/clients/ClientDetailPage.tsx'
import { ClientsPage } from '../modules/clients/ClientsPage.tsx'
import { ProjectDetailPage } from '../modules/projects/ProjectDetailPage.tsx'
import { ProjectsPage } from '../modules/projects/ProjectsPage.tsx'
import { CalculatorTemplateDetailPage } from '../modules/calculator/CalculatorTemplateDetailPage.tsx'
import { CalculatorTemplatesPage } from '../modules/calculator/CalculatorTemplatesPage.tsx'
import { OrderDetailPage } from '../modules/orders/OrderDetailPage.tsx'
import { OrderTypesPage } from '../modules/orders/OrderTypesPage.tsx'
import { OrdersPage } from '../modules/orders/OrdersPage.tsx'
import { ClientsReportPage } from '../modules/reports/ClientsReportPage.tsx'
import { CostsReportPage } from '../modules/reports/CostsReportPage.tsx'
import { OrdersReportPage } from '../modules/reports/OrdersReportPage.tsx'
import { OrderTypesReportPage } from '../modules/reports/OrderTypesReportPage.tsx'
import { ProjectsReportPage } from '../modules/reports/ProjectsReportPage.tsx'
import { ReportsPage } from '../modules/reports/ReportsPage.tsx'

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
          { index: true, element: <Navigate to="/dashboard" replace /> },
          { path: 'dashboard', element: <DashboardPage /> },
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
          {
            path: 'clients',
            element: (
              <PermissionRoute permission={PermissionCodes.clientsView}>
                <ClientsPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'clients/:clientId',
            element: (
              <PermissionRoute permission={PermissionCodes.clientsView}>
                <ClientDetailPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'projects',
            element: (
              <PermissionRoute permission={PermissionCodes.projectsView}>
                <ProjectsPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'projects/:projectId',
            element: (
              <PermissionRoute permission={PermissionCodes.projectsView}>
                <ProjectDetailPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'orders',
            element: (
              <PermissionRoute permission={PermissionCodes.ordersView}>
                <OrdersPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'orders/:orderId',
            element: (
              <PermissionRoute permission={PermissionCodes.ordersView}>
                <OrderDetailPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <ReportsPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports/orders',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <OrdersReportPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports/projects',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <ProjectsReportPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports/clients',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <ClientsReportPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports/order-types',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <OrderTypesReportPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'reports/costs',
            element: (
              <PermissionRoute permission={PermissionCodes.reportsView}>
                <CostsReportPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'admin/order-types',
            element: (
              <PermissionRoute permission={PermissionCodes.ordersManageTypes}>
                <OrderTypesPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'admin/calculator-templates',
            element: (
              <PermissionRoute
                anyOf={[
                  PermissionCodes.calculatorView,
                  PermissionCodes.calculatorManageTemplates,
                  PermissionCodes.calculatorPublishTemplates,
                ]}
              >
                <CalculatorTemplatesPage />
              </PermissionRoute>
            ),
          },
          {
            path: 'admin/calculator-templates/:templateId',
            element: (
              <PermissionRoute
                anyOf={[
                  PermissionCodes.calculatorView,
                  PermissionCodes.calculatorManageTemplates,
                  PermissionCodes.calculatorPublishTemplates,
                ]}
              >
                <CalculatorTemplateDetailPage />
              </PermissionRoute>
            ),
          },
          { path: '*', element: <Navigate to="/dashboard" replace /> },
        ],
      },
    ],
  },
])
