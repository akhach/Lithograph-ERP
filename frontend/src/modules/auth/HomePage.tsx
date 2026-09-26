import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { useAuth } from '../auth/authContext.ts'

export function HomePage() {
  const auth = useAuth()

  return (
    <Paper variant="outlined" sx={{ p: 3 }}>
      <Stack spacing={1}>
        <Typography variant="h5" component="h2">
          Signed in as {auth.user?.username}
        </Typography>
        <Typography color="text.secondary">
          Roles: {auth.user?.roles.join(', ') || 'None'}
        </Typography>
      </Stack>
    </Paper>
  )
}
