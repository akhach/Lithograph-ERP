import CssBaseline from '@mui/material/CssBaseline'
import { ThemeProvider } from '@mui/material/styles'
import { RouterProvider } from 'react-router'
import { appRouter } from '../routes/appRouter.tsx'
import { theme } from '../theme/theme.ts'

export function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <RouterProvider router={appRouter} />
    </ThemeProvider>
  )
}
