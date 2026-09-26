import { createBrowserRouter } from 'react-router'
import { DevelopmentStatusPage } from '../app/DevelopmentStatusPage.tsx'
import { AppLayout } from '../layouts/AppLayout.tsx'

export const appRouter = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [{ index: true, element: <DevelopmentStatusPage /> }],
  },
])
