import Box from '@mui/material/Box'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import type { ReactNode } from 'react'

export function AuthScreen({ title, children }: { title: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2,
      }}
    >
      <Paper variant="outlined" sx={{ width: '100%', maxWidth: 420, p: 3 }}>
        <Typography variant="h5" component="h1" sx={{ mb: 2 }}>
          {title}
        </Typography>
        {children}
      </Paper>
    </Box>
  )
}
