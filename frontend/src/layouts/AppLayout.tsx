import AppBar from '@mui/material/AppBar'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Container from '@mui/material/Container'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useState } from 'react'
import { Link, Outlet } from 'react-router'
import { ChangePasswordDialog } from '../modules/auth/ChangePasswordDialog.tsx'
import { useAuth } from '../modules/auth/authContext.ts'
import { PermissionCodes } from '../modules/auth/authTypes.ts'

export function AppLayout() {
  const auth = useAuth()
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)
  const [changingPassword, setChangingPassword] = useState(false)

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <AppBar position="static">
        <Toolbar variant="dense" sx={{ gap: 1 }}>
          <Typography variant="h6" component="h1" sx={{ mr: 2 }}>
            Lithograph ERP
          </Typography>
          <Button color="inherit" component={Link} to="/">
            Home
          </Button>
          {auth.hasPermission(PermissionCodes.clientsView) ? (
            <Button color="inherit" component={Link} to="/clients">
              Clients
            </Button>
          ) : null}
          {auth.hasPermission(PermissionCodes.usersView) ? (
            <Button color="inherit" component={Link} to="/users">
              Users
            </Button>
          ) : null}
          {auth.hasPermission(PermissionCodes.rolesView) ? (
            <Button color="inherit" component={Link} to="/roles">
              Roles
            </Button>
          ) : null}
          {auth.hasPermission(PermissionCodes.employeesView) ? (
            <Button color="inherit" component={Link} to="/admin/employees">
              Employees
            </Button>
          ) : null}
          <Box sx={{ flexGrow: 1 }} />
          <Button color="inherit" onClick={(event) => setMenuAnchor(event.currentTarget)}>
            {auth.user?.username}
          </Button>
          <Menu
            anchorEl={menuAnchor}
            open={Boolean(menuAnchor)}
            onClose={() => setMenuAnchor(null)}
          >
            <MenuItem
              onClick={() => {
                setMenuAnchor(null)
                setChangingPassword(true)
              }}
            >
              Change password
            </MenuItem>
            <MenuItem
              onClick={() => {
                setMenuAnchor(null)
                void auth.signOut()
              }}
            >
              Log out
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>
      <Container component="main" maxWidth="lg" sx={{ py: 3 }}>
        <Outlet />
      </Container>
      <ChangePasswordDialog open={changingPassword} onClose={() => setChangingPassword(false)} />
    </Box>
  )
}
